using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Messaging;
using UnifiedRewards.Messaging.Events;
using UnifiedRewards.ReportingCompliance.Domain;
using UnifiedRewards.ReportingCompliance.Persistence;

namespace UnifiedRewards.ReportingCompliance.Handlers;

/// <summary>
/// Evolves the Step-1 log handler into a real event-sourced audit store. Each claim lifecycle event
/// is persisted as an immutable AuditEntry in the reporting service's own SQLite DB. The EventId
/// unique index makes this safe under at-least-once delivery — duplicate events are silently ignored.
/// </summary>
public sealed class ClaimEventLogHandler : IIntegrationEventHandler
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly ReportingDbContext _db;
    private readonly ILogger<ClaimEventLogHandler> _logger;

    public ClaimEventLogHandler(ReportingDbContext db, ILogger<ClaimEventLogHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(IntegrationEvent @event, CancellationToken ct)
    {
        // Idempotency: unique index on EventId; skip if already recorded.
        if (await _db.AuditEntries.AnyAsync(a => a.EventId == @event.EventId, ct)) return;

        var entry = @event.EventType switch
        {
            nameof(ClaimSubmitted) => Map(@event, JsonSerializer.Deserialize<ClaimSubmitted>(@event.Payload, Json)),
            nameof(ClaimApproved)  => Map(@event, JsonSerializer.Deserialize<ClaimApproved>(@event.Payload, Json)),
            nameof(ClaimRejected)  => Map(@event, JsonSerializer.Deserialize<ClaimRejected>(@event.Payload, Json)),
            nameof(ClaimSettled)   => Map(@event, JsonSerializer.Deserialize<ClaimSettled>(@event.Payload, Json)),
            nameof(BonusAwarded)   => Map(@event, JsonSerializer.Deserialize<BonusAwarded>(@event.Payload, Json)),
            _ => null   // operational events (DocumentProcessed, SettlementRequested, etc.) not audited here
        };

        if (entry is null) return;

        _db.AuditEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Audit: {EventType} for claim {ClaimId} (tenant {TenantId}).",
            @event.EventType, entry.ClaimId, @event.TenantId);
    }

    private static AuditEntry Base(IntegrationEvent @event, Guid claimId) => new()
    {
        TenantId = @event.TenantId,
        EventId = @event.EventId,
        EventType = @event.EventType,
        ClaimId = claimId,
        OccurredAtUtc = @event.OccurredAtUtc,
    };

    private static AuditEntry? Map(IntegrationEvent ev, ClaimSubmitted? e)
    {
        if (e is null) return null;
        var a = Base(ev, e.ClaimId);
        a.ActorId = e.EmployeeId;
        a.Amount = e.Amount;
        return a;
    }

    private static AuditEntry? Map(IntegrationEvent ev, ClaimApproved? e)
    {
        if (e is null) return null;
        var a = Base(ev, e.ClaimId);
        a.ActorId = e.ReviewerId;
        a.Amount = e.Amount;
        return a;
    }

    private static AuditEntry? Map(IntegrationEvent ev, ClaimRejected? e)
    {
        if (e is null) return null;
        var a = Base(ev, e.ClaimId);
        a.ActorId = e.ReviewerId;
        a.Notes = e.Reason;
        return a;
    }

    private static AuditEntry? Map(IntegrationEvent ev, ClaimSettled? e)
    {
        if (e is null) return null;
        var a = Base(ev, e.ClaimId);
        a.ActorId = e.EmployeeId;
        a.Notes = e.SettlementReference;
        return a;
    }

    private static AuditEntry? Map(IntegrationEvent ev, BonusAwarded? e)
    {
        if (e is null) return null;
        // ClaimId field repurposed as NominationId — the unique reference for this bonus award.
        var a = Base(ev, e.NominationId);
        a.ActorId = e.EmployeeId;
        a.Amount = e.Amount;
        a.Notes = $"Campaign: {e.CampaignId:N}";
        return a;
    }
}
