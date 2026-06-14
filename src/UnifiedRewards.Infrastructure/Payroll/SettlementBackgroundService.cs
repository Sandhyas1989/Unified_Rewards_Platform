using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UnifiedRewards.Infrastructure.Payroll;

/// <summary>
/// Local/default async settlement worker: drains the in-memory channel queue and hands each id to
/// the shared <see cref="SettlementProcessor"/>. (In production this is replaced by the Service Bus
/// consumer; see ServiceBusSettlementConsumer.)
/// </summary>
public sealed class SettlementBackgroundService : BackgroundService
{
    private readonly SettlementChannelQueue _queue;
    private readonly SettlementProcessor _processor;
    private readonly ILogger<SettlementBackgroundService> _logger;

    public SettlementBackgroundService(
        SettlementChannelQueue queue,
        SettlementProcessor processor,
        ILogger<SettlementBackgroundService> logger)
    {
        _queue = queue;
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var settlementId in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _processor.ProcessAsync(settlementId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing settlement {SettlementId}", settlementId);
            }
        }
    }
}
