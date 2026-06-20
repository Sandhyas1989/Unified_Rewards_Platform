namespace UnifiedRewards.Messaging;

/// <summary>
/// The envelope that travels on the bus — the local stand-in for an Azure Service Bus message.
/// <see cref="EventId"/> is the idempotency key (consumers dedupe on it); <see cref="Payload"/> is the
/// JSON-serialized typed event (see Events.cs). <see cref="EventType"/> is the event record's type name.
/// </summary>
public sealed record IntegrationEvent(
    Guid EventId,
    string EventType,
    Guid TenantId,
    DateTime OccurredAtUtc,
    string Payload);

/// <summary>
/// Publishes integration events. Mirrors the monolith's IEventBus seam.
/// IMPORTANT: a publish does NOT send immediately — it STAGES the event in the caller's current
/// unit of work (the transactional outbox). The event is committed atomically by the caller's
/// existing SaveChangesAsync, then shipped to the bus asynchronously by the outbox dispatcher.
/// This guarantees a domain change and its event are never out of sync, even if the bus is down.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, Guid tenantId, CancellationToken ct = default)
        where TEvent : class;
}

/// <summary>
/// Handles integration events delivered to a subscribing service. Implementations MUST be idempotent
/// (delivery is at-least-once). A handler inspects <see cref="IntegrationEvent.EventType"/> and ignores
/// events it does not care about.
/// </summary>
public interface IIntegrationEventHandler
{
    Task HandleAsync(IntegrationEvent @event, CancellationToken ct);
}
