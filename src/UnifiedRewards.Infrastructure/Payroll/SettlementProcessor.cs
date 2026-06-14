using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Common.Interfaces.Integration;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Infrastructure.Payroll;

/// <summary>
/// Processes one settlement request: resolves a DI scope, pushes to payroll (through the resilient
/// IPayrollService), and records the outcome. Shared by the in-memory worker and the Service Bus
/// consumer so the logic — including the idempotency guard — lives in exactly one place.
/// </summary>
public sealed class SettlementProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SettlementProcessor> _logger;

    public SettlementProcessor(IServiceScopeFactory scopeFactory, ILogger<SettlementProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid settlementId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var payroll = scope.ServiceProvider.GetRequiredService<IPayrollService>();

        var settlement = await db.SettlementRequests.FindAsync(new object[] { settlementId }, cancellationToken);
        if (settlement is null)
        {
            _logger.LogWarning("Settlement {SettlementId} not found; skipping.", settlementId);
            return;
        }

        // Idempotency guard: with at-least-once delivery the same id can arrive more than once.
        if (settlement.Status != SettlementStatus.Pending)
        {
            _logger.LogInformation("Settlement {Reference} already {Status}; skipping (idempotent).",
                settlement.Reference, settlement.Status);
            return;
        }

        settlement.MarkProcessing();
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var ok = await payroll.PushSettlementAsync(
                settlement.EmployeeId, settlement.Amount, settlement.Reference, cancellationToken);
            if (ok)
            {
                settlement.MarkSucceeded($"PAYROLL-{settlement.Reference}");
                _logger.LogInformation("Settlement {Reference} succeeded.", settlement.Reference);
            }
            else
            {
                settlement.MarkFailed("Payroll rejected the settlement.");
            }
        }
        catch (Exception ex)
        {
            settlement.MarkFailed(ex.Message);
            _logger.LogError(ex, "Settlement {Reference} failed after resilience.", settlement.Reference);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
