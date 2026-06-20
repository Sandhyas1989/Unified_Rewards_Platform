using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace UnifiedRewards.Messaging.Outbox;

/// <summary>
/// A staged-but-not-yet-shipped integration event, persisted in the OWNING service's own database
/// (db-per-service preserved). Written in the same transaction as the domain change that produced it.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();   // also the IntegrationEvent.EventId
    public string EventType { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DispatchedAtUtc { get; set; }   // null = awaiting dispatch
    public int Attempts { get; set; }
}

public static class OutboxModelBuilderExtensions
{
    /// <summary>Call from each service's DbContext.OnModelCreating to map the outbox table.</summary>
    public static ModelBuilder ApplyOutbox(this ModelBuilder b)
    {
        b.Entity<OutboxMessage>(e =>
        {
            e.ToTable("OutboxMessages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.EventType).HasMaxLength(200);
            e.HasIndex(x => x.DispatchedAtUtc);
        });
        return b;
    }
}

/// <summary>
/// IEventBus implementation backed by the transactional outbox. PublishAsync stages an OutboxMessage on
/// the current scoped DbContext; the caller's SaveChangesAsync commits it atomically with domain state.
/// Transport-agnostic — the same staging is used for both Local and Azure Service Bus.
/// </summary>
public sealed class OutboxEventBus : IEventBus
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly DbContext _db;

    public OutboxEventBus(DbContext db) => _db = db;

    public Task PublishAsync<TEvent>(TEvent @event, Guid tenantId, CancellationToken ct = default)
        where TEvent : class
    {
        // Use the runtime type so EventType/payload are correct even when TEvent is inferred as object.
        var type = @event.GetType();
        _db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            EventType = type.Name,
            TenantId = tenantId,
            Payload = JsonSerializer.Serialize(@event, type, Json),
            OccurredAtUtc = DateTime.UtcNow,
        });
        // Intentionally NOT saving here — the caller's unit of work commits it.
        return Task.CompletedTask;
    }
}
