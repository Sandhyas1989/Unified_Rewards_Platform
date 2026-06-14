namespace UnifiedRewards.Application.Common.Interfaces;

/// <summary>
/// Hands a persisted settlement request off for asynchronous background processing.
/// Implemented in Infrastructure with an in-process channel (Azure Service Bus in cloud).
/// </summary>
public interface ISettlementQueue
{
    ValueTask EnqueueAsync(Guid settlementId, CancellationToken cancellationToken = default);
}
