namespace UnifiedRewards.ReportingCompliance.Domain;

public class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }       // IntegrationEvent.EventId — idempotency key
    public string EventType { get; set; } = string.Empty;
    public Guid ClaimId { get; set; }
    public Guid? ActorId { get; set; }      // employee, reviewer, or system actor depending on event
    public decimal? Amount { get; set; }
    public string? CurrencyCode { get; set; }
    public string? Notes { get; set; }      // decision notes, rejection reason, or settlement reference
    public DateTime OccurredAtUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
