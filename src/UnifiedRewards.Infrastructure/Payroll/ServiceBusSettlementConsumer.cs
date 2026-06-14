using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UnifiedRewards.Infrastructure.Payroll;

/// <summary>
/// Production async settlement worker: a competing consumer over an Azure Service Bus queue. Each
/// received message is handed to the shared <see cref="SettlementProcessor"/>; on success the
/// message is completed, otherwise it is abandoned for redelivery. Enabled only when
/// "Messaging:Provider" = "ServiceBus".
/// </summary>
public sealed class ServiceBusSettlementConsumer : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly SettlementProcessor _settlementProcessor;
    private readonly ILogger<ServiceBusSettlementConsumer> _logger;

    public ServiceBusSettlementConsumer(
        ServiceBusClient client,
        string queueName,
        SettlementProcessor settlementProcessor,
        ILogger<ServiceBusSettlementConsumer> logger)
    {
        _processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions { AutoCompleteMessages = false });
        _settlementProcessor = settlementProcessor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += async args =>
        {
            if (Guid.TryParse(args.Message.Body.ToString(), out var id))
            {
                await _settlementProcessor.ProcessAsync(id, args.CancellationToken);
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            }
            else
            {
                _logger.LogWarning("Settlement message {MessageId} had an unparseable body; dead-lettering.", args.Message.MessageId);
                await args.DeadLetterMessageAsync(args.Message, "UnparseableBody", cancellationToken: args.CancellationToken);
            }
        };
        _processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception, "Service Bus settlement processing error ({Source}).", args.ErrorSource);
            return Task.CompletedTask;
        };

        await _processor.StartProcessingAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
