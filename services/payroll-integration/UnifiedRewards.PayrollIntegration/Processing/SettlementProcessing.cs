using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Messaging;
using UnifiedRewards.Messaging.Events;
using UnifiedRewards.PayrollIntegration.Domain;
using UnifiedRewards.PayrollIntegration.Integration;
using UnifiedRewards.PayrollIntegration.Persistence;

namespace UnifiedRewards.PayrollIntegration.Processing;

// In-process channel queue (local/default). Azure Service Bus is the production swap.
public sealed class SettlementQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ChannelReader<Guid> Reader => _channel.Reader;
    public ValueTask EnqueueAsync(Guid id, CancellationToken ct = default) => _channel.Writer.WriteAsync(id, ct);
}

// Processes one settlement: pushes to payroll (through the resilient gateway) and records the outcome.
public sealed class SettlementProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SettlementProcessor> _logger;

    public SettlementProcessor(IServiceScopeFactory scopeFactory, ILogger<SettlementProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid settlementId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PayrollDbContext>();
        var gateway = scope.ServiceProvider.GetRequiredService<IPayrollGateway>();
        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var s = await db.Settlements.FirstOrDefaultAsync(x => x.Id == settlementId, ct);
        if (s is null) { _logger.LogWarning("Settlement {Id} not found.", settlementId); return; }

        // Idempotency guard (safe under at-least-once delivery).
        if (s.Status != SettlementStatus.Pending)
        {
            _logger.LogInformation("Settlement {Ref} already {Status}; skipping (idempotent).", s.Reference, s.Status);
            return;
        }

        s.MarkProcessing();
        await db.SaveChangesAsync(ct);

        try
        {
            var ok = await gateway.PushSettlementAsync(s.EmployeeId, s.Amount, s.Reference, ct);
            if (ok) { s.MarkSucceeded($"PAYROLL-{s.Reference}"); _logger.LogInformation("Settlement {Ref} succeeded.", s.Reference); }
            else { s.MarkFailed("Payroll rejected the settlement."); }
        }
        catch (Exception ex)
        {
            s.MarkFailed(ex.Message);
            _logger.LogError(ex, "Settlement {Ref} failed after resilience.", s.Reference);
        }

        // Stage SettlementCompleted in the same transaction as the final status row — atomic with the DB state change.
        await bus.PublishAsync(
            new SettlementCompleted(s.ClaimId, s.Id, s.Status == SettlementStatus.Succeeded,
                s.PayrollConfirmation ?? string.Empty, s.LastError, DateTime.UtcNow),
            s.TenantId, ct);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class SettlementBackgroundService : BackgroundService
{
    private readonly SettlementQueue _queue;
    private readonly SettlementProcessor _processor;
    private readonly ILogger<SettlementBackgroundService> _logger;

    public SettlementBackgroundService(SettlementQueue queue, SettlementProcessor processor, ILogger<SettlementBackgroundService> logger)
    {
        _queue = queue; _processor = processor; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var id in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Use a per-item timeout rather than stoppingToken so a graceful shutdown does not
                // orphan a settlement mid-payment (claim stuck in Processing with no SettlementCompleted).
                using var itemCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await _processor.ProcessAsync(id, itemCts.Token);
            }
            catch (OperationCanceledException) { _logger.LogWarning("Settlement {Id} timed out (>30 s); will retry on next start.", id); }
            catch (Exception ex) { _logger.LogError(ex, "Error processing settlement {Id}", id); }
        }
    }
}
