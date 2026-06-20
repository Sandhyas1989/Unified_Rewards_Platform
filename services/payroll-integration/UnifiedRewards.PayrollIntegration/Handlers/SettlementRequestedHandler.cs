using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Messaging;
using UnifiedRewards.Messaging.Events;
using UnifiedRewards.PayrollIntegration.Domain;
using UnifiedRewards.PayrollIntegration.Persistence;
using UnifiedRewards.PayrollIntegration.Processing;

namespace UnifiedRewards.PayrollIntegration.Handlers;

/// <summary>
/// Settlement leg of the reimbursement saga: when Reimbursement publishes SettlementRequested, create
/// a SettlementRequest row and hand it to the in-process queue for async processing. Idempotent —
/// an in-flight or already-succeeded settlement for the same claim is silently ignored; a previously
/// failed settlement triggers a retry (new SettlementRequest row).
/// </summary>
public sealed class SettlementRequestedHandler : IIntegrationEventHandler
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly PayrollDbContext _db;
    private readonly SettlementQueue _queue;
    private readonly ILogger<SettlementRequestedHandler> _logger;

    public SettlementRequestedHandler(PayrollDbContext db, SettlementQueue queue, ILogger<SettlementRequestedHandler> logger)
    {
        _db = db;
        _queue = queue;
        _logger = logger;
    }

    public async Task HandleAsync(IntegrationEvent @event, CancellationToken ct)
    {
        if (@event.EventType != nameof(SettlementRequested)) return;
        var e = JsonSerializer.Deserialize<SettlementRequested>(@event.Payload, Json);
        if (e is null) return;

        // Idempotency: skip if an active (non-failed) settlement already exists for this claim.
        var active = await _db.Settlements.FirstOrDefaultAsync(
            s => s.ClaimId == e.ClaimId && s.TenantId == @event.TenantId && s.Status != SettlementStatus.Failed, ct);
        if (active is not null)
        {
            _logger.LogInformation("Active settlement {Id} already exists for claim {ClaimId} ({Status}); skipping.",
                active.Id, e.ClaimId, active.Status);
            return;
        }

        var settlement = new SettlementRequest
        {
            TenantId = @event.TenantId,
            ClaimId = e.ClaimId,
            EmployeeId = e.EmployeeId,
            Amount = e.Amount,
            Reference = $"CLM-{e.ClaimId:N}",
        };
        _db.Settlements.Add(settlement);
        await _db.SaveChangesAsync(ct);

        await _queue.EnqueueAsync(settlement.Id, ct);
        _logger.LogInformation("Settlement {Id} enqueued for claim {ClaimId}.", settlement.Id, e.ClaimId);
    }
}
