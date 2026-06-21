using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using UnifiedRewards.ReportingCompliance.Domain;

namespace UnifiedRewards.ReportingCompliance.Handlers;

/// <summary>
/// Streams each immutable AuditEntry to Azure Event Hub (the append-only audit stream).
/// No-op when no Event Hub connection string is configured (local/dev), so existing behaviour
/// is unchanged until ConnectionStrings:EventHub is set.
/// </summary>
public sealed class AuditStreamPublisher : IAsyncDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly EventHubProducerClient? _producer;

    public AuditStreamPublisher(IConfiguration config)
    {
        var conn = config.GetConnectionString("EventHub");
        if (!string.IsNullOrWhiteSpace(conn))
            _producer = new EventHubProducerClient(conn, "audit-stream");
    }

    public async Task PublishAsync(AuditEntry entry, CancellationToken ct)
    {
        if (_producer is null) return;
        var data = new EventData(JsonSerializer.Serialize(entry, Json));
        // Partition by tenant so each tenant's audit trail stays ordered.
        await _producer.SendAsync(
            new[] { data },
            new SendEventOptions { PartitionKey = entry.TenantId.ToString() },
            ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_producer is not null) await _producer.DisposeAsync();
    }
}
