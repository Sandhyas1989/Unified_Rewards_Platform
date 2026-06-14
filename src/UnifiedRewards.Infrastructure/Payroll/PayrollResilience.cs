using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using UnifiedRewards.Application.Common.Interfaces.Integration;
using UnifiedRewards.Infrastructure.Integration;

namespace UnifiedRewards.Infrastructure.Payroll;

/// <summary>Builds the shared Polly v8 resilience pipeline for payroll pushes.</summary>
public static class PayrollResilience
{
    public static ResiliencePipeline<bool> BuildPipeline(ILogger logger)
    {
        var handle = new PredicateBuilder<bool>()
            .Handle<TransientPayrollException>()
            .HandleResult(success => success == false);

        return new ResiliencePipelineBuilder<bool>()
            .AddRetry(new RetryStrategyOptions<bool>
            {
                ShouldHandle = handle,
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Payroll push transient failure; retry {Attempt} in {Delay}.",
                        args.AttemptNumber + 1, args.RetryDelay);
                    return default;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<bool>
            {
                ShouldHandle = handle,
                FailureRatio = 0.9,
                MinimumThroughput = 20,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(15)
            })
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
    }
}

/// <summary>
/// Decorates the payroll integration with the Polly pipeline so every push (synchronous claim
/// settlement and the async settlement worker alike) gets retry / circuit-breaker / timeout.
/// </summary>
public sealed class ResilientPayrollService : IPayrollService
{
    private readonly IPayrollService _inner;
    private readonly ResiliencePipeline<bool> _pipeline;

    public ResilientPayrollService(MockPayrollService inner, ResiliencePipeline<bool> pipeline)
    {
        _inner = inner;
        _pipeline = pipeline;
    }

    public async Task<bool> PushSettlementAsync(Guid employeeId, decimal amount, string reference, CancellationToken cancellationToken = default)
        => await _pipeline.ExecuteAsync(
            async token => await _inner.PushSettlementAsync(employeeId, amount, reference, token),
            cancellationToken);
}
