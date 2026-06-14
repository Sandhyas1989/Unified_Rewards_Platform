using System.Collections.Concurrent;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace UnifiedRewards.PayrollIntegration.Integration;

// Seam to the external payroll system (ported from the monolith). The resilient decorator wraps the
// mock with a Polly pipeline; in production this calls the real payroll REST API.
public interface IPayrollGateway
{
    Task<bool> PushSettlementAsync(Guid employeeId, decimal amount, string reference, CancellationToken ct = default);
}

public sealed class TransientPayrollException : Exception
{
    public TransientPayrollException(string message) : base(message) { }
}

// Mock external system: fails the FIRST attempt per reference (transient) then succeeds, so the
// Polly retry is genuinely exercised — same demonstrable behaviour as the monolith.
public sealed class MockPayrollGateway : IPayrollGateway
{
    private static readonly ConcurrentDictionary<string, int> Attempts = new();
    private readonly ILogger<MockPayrollGateway> _logger;

    public MockPayrollGateway(ILogger<MockPayrollGateway> logger) => _logger = logger;

    public Task<bool> PushSettlementAsync(Guid employeeId, decimal amount, string reference, CancellationToken ct = default)
    {
        var attempt = Attempts.AddOrUpdate(reference, 1, (_, c) => c + 1);
        _logger.LogInformation("[Payroll -> external] attempt {Attempt} Employee={Employee} Amount={Amount} Ref={Reference}",
            attempt, employeeId, amount, reference);
        if (attempt == 1)
        {
            throw new TransientPayrollException($"Transient payroll fault on attempt {attempt} for {reference}.");
        }
        return Task.FromResult(true);
    }
}

public static class PayrollResilience
{
    public static ResiliencePipeline<bool> BuildPipeline(ILogger logger) =>
        new ResiliencePipelineBuilder<bool>()
            .AddRetry(new RetryStrategyOptions<bool>
            {
                ShouldHandle = new PredicateBuilder<bool>().Handle<TransientPayrollException>().HandleResult(ok => ok == false),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    logger.LogWarning("Payroll push transient failure; retry {Attempt} in {Delay}.", args.AttemptNumber, args.RetryDelay);
                    return default;
                },
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<bool>
            {
                ShouldHandle = new PredicateBuilder<bool>().Handle<TransientPayrollException>().HandleResult(ok => ok == false),
                FailureRatio = 0.9,
                MinimumThroughput = 20,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(15),
            })
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
}

public sealed class ResilientPayrollGateway : IPayrollGateway
{
    private readonly MockPayrollGateway _inner;
    private readonly ResiliencePipeline<bool> _pipeline;

    public ResilientPayrollGateway(MockPayrollGateway inner, ResiliencePipeline<bool> pipeline)
    {
        _inner = inner;
        _pipeline = pipeline;
    }

    public async Task<bool> PushSettlementAsync(Guid employeeId, decimal amount, string reference, CancellationToken ct = default)
        => await _pipeline.ExecuteAsync(async token => await _inner.PushSettlementAsync(employeeId, amount, reference, token), ct);
}
