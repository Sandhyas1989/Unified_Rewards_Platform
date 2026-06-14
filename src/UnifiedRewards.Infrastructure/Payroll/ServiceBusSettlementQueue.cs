using Azure.Messaging.ServiceBus;
using UnifiedRewards.Application.Common.Interfaces;

namespace UnifiedRewards.Infrastructure.Payroll;

/// <summary>
/// Production settlement queue backed by Azure Service Bus. Enabled by configuration
/// ("Messaging:Provider" = "ServiceBus"); the default local profile uses the in-memory channel.
/// Messages are durable and survive restarts, and multiple instances act as competing consumers.
/// </summary>
public sealed class ServiceBusSettlementQueue : ISettlementQueue, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;

    public ServiceBusSettlementQueue(ServiceBusClient client, string queueName)
    {
        _sender = client.CreateSender(queueName);
    }

    public async ValueTask EnqueueAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        var message = new ServiceBusMessage(settlementId.ToString())
        {
            // De-dup hint for Service Bus (when duplicate detection is enabled on the queue).
            MessageId = settlementId.ToString(),
        };
        await _sender.SendMessageAsync(message, cancellationToken);
    }

    public ValueTask DisposeAsync() => _sender.DisposeAsync();
}
