using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnifiedRewards.Messaging.Outbox;

namespace UnifiedRewards.Messaging.Azure;

/// <summary>Deploy-time transport config. The outbox pattern is unchanged from Local — only the
/// dispatcher target (a Service Bus topic) and the consumer (a subscription) differ.</summary>
public sealed record ServiceBusOptions(string ConnectionString, string Topic, string? Subscription);

/// <summary>Ships staged outbox rows to an Azure Service Bus topic. MessageId = EventId enables
/// Service Bus duplicate detection on top of the consumer's own idempotency.</summary>
public sealed class ServiceBusOutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ServiceBusOptions _opts;
    private readonly ILogger<ServiceBusOutboxDispatcher> _logger;

    public ServiceBusOutboxDispatcher(IServiceScopeFactory scopes, ServiceBusOptions opts, ILogger<ServiceBusOutboxDispatcher> logger)
    { _scopes = scopes; _opts = opts; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await using var client = new ServiceBusClient(_opts.ConnectionString);
        await using var sender = client.CreateSender(_opts.Topic);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DbContext>();
                var pending = await db.Set<OutboxMessage>()
                    .Where(m => m.DispatchedAtUtc == null).OrderBy(m => m.OccurredAtUtc).Take(50).ToListAsync(ct);

                foreach (var m in pending)
                {
                    var msg = new ServiceBusMessage(m.Payload)
                    {
                        MessageId = m.Id.ToString(),
                        Subject = m.EventType,
                        ApplicationProperties = { ["EventType"] = m.EventType, ["TenantId"] = m.TenantId.ToString(), ["OccurredAtUtc"] = m.OccurredAtUtc.ToString("O") },
                    };
                    await sender.SendMessageAsync(msg, ct);
                    m.DispatchedAtUtc = DateTime.UtcNow;
                    m.Attempts++;
                }
                if (pending.Count > 0) await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) { _logger.LogError(ex, "Service Bus outbox dispatch error."); }
            await Task.Delay(500, ct);
        }
    }
}

/// <summary>Consumes a Service Bus subscription and invokes registered handlers idempotently.</summary>
public sealed class ServiceBusSubscriber : BackgroundService
{
    private readonly ServiceBusOptions _opts;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ServiceBusSubscriber> _logger;

    public ServiceBusSubscriber(ServiceBusOptions opts, IServiceScopeFactory scopes, ILogger<ServiceBusSubscriber> logger)
    { _opts = opts; _scopes = scopes; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await using var client = new ServiceBusClient(_opts.ConnectionString);
        var processor = client.CreateProcessor(_opts.Topic, _opts.Subscription, new ServiceBusProcessorOptions());

        processor.ProcessMessageAsync += async args =>
        {
            var m = args.Message;
            var evt = new IntegrationEvent(
                Guid.TryParse(m.MessageId, out var id) ? id : Guid.NewGuid(),
                m.ApplicationProperties.TryGetValue("EventType", out var t) ? t?.ToString() ?? m.Subject ?? "" : m.Subject ?? "",
                m.ApplicationProperties.TryGetValue("TenantId", out var tn) && Guid.TryParse(tn?.ToString(), out var g) ? g : Guid.Empty,
                m.ApplicationProperties.TryGetValue("OccurredAtUtc", out var o) && DateTime.TryParse(o?.ToString(), out var dt) ? dt : DateTime.UtcNow,
                m.Body.ToString());

            using var scope = _scopes.CreateScope();
            foreach (var h in scope.ServiceProvider.GetServices<IIntegrationEventHandler>())
                await h.HandleAsync(evt, args.CancellationToken);
            await args.CompleteMessageAsync(m, args.CancellationToken);
        };
        processor.ProcessErrorAsync += err => { _logger.LogError(err.Exception, "Service Bus processor error."); return Task.CompletedTask; };

        await processor.StartProcessingAsync(ct);
        try { await Task.Delay(Timeout.Infinite, ct); }
        catch (TaskCanceledException) { }
        await processor.StopProcessingAsync();
    }
}
