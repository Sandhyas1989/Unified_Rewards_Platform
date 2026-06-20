using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Messaging;
using UnifiedRewards.Messaging.Events;
using UnifiedRewards.ReimbursementWorkflow.Domain;
using UnifiedRewards.ReimbursementWorkflow.Persistence;

namespace UnifiedRewards.ReimbursementWorkflow.Handlers;

/// <summary>
/// Document leg of the reimbursement saga: when a claim's receipt has been stored + OCR'd by the
/// Document service, advance the claim to "In Review". Idempotent — only a still-Submitted claim is
/// transitioned, so duplicate or late deliveries are safely ignored.
/// </summary>
public sealed class DocumentProcessedHandler : IIntegrationEventHandler
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly ReimbursementDbContext _db;
    private readonly ILogger<DocumentProcessedHandler> _logger;

    public DocumentProcessedHandler(ReimbursementDbContext db, ILogger<DocumentProcessedHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(IntegrationEvent @event, CancellationToken ct)
    {
        if (@event.EventType != nameof(DocumentProcessed)) return;
        var e = JsonSerializer.Deserialize<DocumentProcessed>(@event.Payload, Json);
        if (e is null) return;

        var claim = await _db.Claims.FirstOrDefaultAsync(c => c.Id == e.ClaimId && c.TenantId == @event.TenantId, ct);
        if (claim is null) { _logger.LogWarning("DocumentProcessed for unknown claim {ClaimId}.", e.ClaimId); return; }
        if (claim.Status != ClaimStatus.Submitted) return;   // already advanced — idempotent

        claim.StartReview(Guid.Empty);   // system-initiated transition (receipt validated)
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Claim {ClaimId} moved to In Review (receipt processed).", e.ClaimId);
    }
}
