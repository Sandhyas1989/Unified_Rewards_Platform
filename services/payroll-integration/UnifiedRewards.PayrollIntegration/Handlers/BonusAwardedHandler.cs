using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Messaging;
using UnifiedRewards.Messaging.Events;
using UnifiedRewards.PayrollIntegration.Domain;
using UnifiedRewards.PayrollIntegration.Persistence;
using UnifiedRewards.PayrollIntegration.Processing;

namespace UnifiedRewards.PayrollIntegration.Handlers;

/// <summary>
/// Bonus campaign leg: when EmployeeProfile publishes BonusAwarded (HR Admin approved a nomination),
/// create a SettlementRequest for the bonus payout and enqueue for async processing. Idempotent —
/// an in-flight or already-succeeded settlement for the same nomination is silently ignored.
/// </summary>
public sealed class BonusAwardedHandler : IIntegrationEventHandler
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly PayrollDbContext _db;
    private readonly SettlementQueue _queue;
    private readonly ILogger<BonusAwardedHandler> _logger;

    public BonusAwardedHandler(PayrollDbContext db, SettlementQueue queue, ILogger<BonusAwardedHandler> logger)
    {
        _db = db;
        _queue = queue;
        _logger = logger;
    }

    public async Task HandleAsync(IntegrationEvent @event, CancellationToken ct)
    {
        if (@event.EventType != nameof(BonusAwarded)) return;
        var e = JsonSerializer.Deserialize<BonusAwarded>(@event.Payload, Json);
        if (e is null) return;

        // Idempotency: skip if an active (non-failed) settlement already exists for this nomination.
        var active = await _db.Settlements.FirstOrDefaultAsync(
            s => s.ClaimId == e.NominationId && s.TenantId == @event.TenantId && s.Status != SettlementStatus.Failed, ct);
        if (active is not null)
        {
            _logger.LogInformation("Active settlement {Id} already exists for nomination {NominationId} ({Status}); skipping.",
                active.Id, e.NominationId, active.Status);
            return;
        }

        var settlement = new SettlementRequest
        {
            TenantId = @event.TenantId,
            ClaimId = e.NominationId,
            EmployeeId = e.EmployeeId,
            Amount = e.Amount,
            Reference = $"BONUS-{e.NominationId:N}",
        };
        _db.Settlements.Add(settlement);
        await _db.SaveChangesAsync(ct);

        await _queue.EnqueueAsync(settlement.Id, ct);
        _logger.LogInformation("Bonus settlement {Id} enqueued for nomination {NominationId} (employee {EmployeeId}).",
            settlement.Id, e.NominationId, e.EmployeeId);
    }
}
