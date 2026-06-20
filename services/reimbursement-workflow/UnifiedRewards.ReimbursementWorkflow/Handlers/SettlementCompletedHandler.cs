using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Messaging;
using UnifiedRewards.Messaging.Events;
using UnifiedRewards.ReimbursementWorkflow.Domain;
using UnifiedRewards.ReimbursementWorkflow.Persistence;

namespace UnifiedRewards.ReimbursementWorkflow.Handlers;

/// <summary>
/// Settlement leg of the reimbursement saga: when Payroll reports a completed settlement, close the
/// claim (→ Settled) on success. Idempotent — only an Approved claim is settled, so duplicates are
/// ignored; a failure leaves the claim Approved so Finance can retry.
/// </summary>
public sealed class SettlementCompletedHandler : IIntegrationEventHandler
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly ReimbursementDbContext _db;
    private readonly IEventBus _bus;
    private readonly ILogger<SettlementCompletedHandler> _logger;

    public SettlementCompletedHandler(ReimbursementDbContext db, IEventBus bus, ILogger<SettlementCompletedHandler> logger)
    {
        _db = db;
        _bus = bus;
        _logger = logger;
    }

    public async Task HandleAsync(IntegrationEvent @event, CancellationToken ct)
    {
        if (@event.EventType != nameof(SettlementCompleted)) return;
        var e = JsonSerializer.Deserialize<SettlementCompleted>(@event.Payload, Json);
        if (e is null) return;

        var claim = await _db.Claims.FirstOrDefaultAsync(c => c.Id == e.ClaimId && c.TenantId == @event.TenantId, ct);
        if (claim is null) { _logger.LogWarning("SettlementCompleted for unknown claim {ClaimId}.", e.ClaimId); return; }

        if (!e.Success)
        {
            _logger.LogWarning("Settlement failed for claim {ClaimId}: {Error}", e.ClaimId, e.Error);
            return;   // leave claim Approved so Finance can retry
        }
        if (claim.Status != ClaimStatus.Approved) return;   // already settled — idempotent

        claim.Settle(Guid.Empty, e.Reference);
        // Audit/reporting event, staged in the same transaction as the claim's Settled transition.
        await _bus.PublishAsync(new ClaimSettled(claim.Id, claim.EmployeeId, e.Reference, DateTime.UtcNow), @event.TenantId, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Claim {ClaimId} settled ({Reference}).", e.ClaimId, e.Reference);
    }
}
