using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnifiedRewards.Messaging.Outbox;

namespace UnifiedRewards.Messaging.Local;

/// <summary>
/// The local "bus": a shared SQLite file all services read/write, standing in for an Azure Service Bus
/// TOPIC. Append-only message log + per-subscriber offsets (the stand-in for a SUBSCRIPTION). Works across
/// processes (each service is its own `dotnet run`); WAL mode allows concurrent readers/writers.
/// Disappears entirely in Azure (Provider=ServiceBus).
/// </summary>
public sealed class SqliteMessageLog
{
    private readonly string _connString;

    public SqliteMessageLog(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _connString = new SqliteConnectionStringBuilder { DataSource = path, Cache = SqliteCacheMode.Shared }.ToString();
        using var c = Open();
        Exec(c, "PRAGMA journal_mode=WAL;");
        Exec(c, @"CREATE TABLE IF NOT EXISTS messages(
                    Seq INTEGER PRIMARY KEY AUTOINCREMENT, EventId TEXT NOT NULL, EventType TEXT NOT NULL,
                    TenantId TEXT NOT NULL, Payload TEXT NOT NULL, OccurredAtUtc TEXT NOT NULL);");
        Exec(c, "CREATE TABLE IF NOT EXISTS consumer_offsets(Subscriber TEXT PRIMARY KEY, LastSeq INTEGER NOT NULL);");
    }

    private SqliteConnection Open() { var c = new SqliteConnection(_connString); c.Open(); return c; }
    private static void Exec(SqliteConnection c, string sql) { using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery(); }

    public void Append(IntegrationEvent e)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"INSERT INTO messages(EventId,EventType,TenantId,Payload,OccurredAtUtc)
                            VALUES($id,$type,$tenant,$payload,$at);";
        cmd.Parameters.AddWithValue("$id", e.EventId.ToString());
        cmd.Parameters.AddWithValue("$type", e.EventType);
        cmd.Parameters.AddWithValue("$tenant", e.TenantId.ToString());
        cmd.Parameters.AddWithValue("$payload", e.Payload);
        cmd.Parameters.AddWithValue("$at", e.OccurredAtUtc.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public long GetOffset(string subscriber)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT LastSeq FROM consumer_offsets WHERE Subscriber=$s;";
        cmd.Parameters.AddWithValue("$s", subscriber);
        return cmd.ExecuteScalar() is long v ? v : 0L;
    }

    public void SetOffset(string subscriber, long seq)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"INSERT INTO consumer_offsets(Subscriber,LastSeq) VALUES($s,$seq)
                            ON CONFLICT(Subscriber) DO UPDATE SET LastSeq=$seq;";
        cmd.Parameters.AddWithValue("$s", subscriber);
        cmd.Parameters.AddWithValue("$seq", seq);
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<(long Seq, IntegrationEvent Event)> ReadAfter(long afterSeq, int max)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"SELECT Seq,EventId,EventType,TenantId,Payload,OccurredAtUtc FROM messages
                            WHERE Seq>$after ORDER BY Seq LIMIT $max;";
        cmd.Parameters.AddWithValue("$after", afterSeq);
        cmd.Parameters.AddWithValue("$max", max);
        var list = new List<(long, IntegrationEvent)>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add((r.GetInt64(0), new IntegrationEvent(
                Guid.Parse(r.GetString(1)), r.GetString(2), Guid.Parse(r.GetString(3)),
                DateTime.Parse(r.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind), r.GetString(4))));
        return list;
    }
}

/// <summary>Ships staged outbox rows from the owning service's DB to the local message-log bus.</summary>
public sealed class LocalOutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly SqliteMessageLog _log;
    private readonly ILogger<LocalOutboxDispatcher> _logger;

    public LocalOutboxDispatcher(IServiceScopeFactory scopes, SqliteMessageLog log, ILogger<LocalOutboxDispatcher> logger)
    { _scopes = scopes; _log = log; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await PumpOnceAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Outbox dispatch loop error."); }
            await Task.Delay(500, ct);
        }
    }

    /// <summary>One pass: ship all undispatched outbox rows to the bus and mark them. Returns the count.</summary>
    public async Task<int> PumpOnceAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();
        var pending = await db.Set<OutboxMessage>()
            .Where(m => m.DispatchedAtUtc == null).OrderBy(m => m.OccurredAtUtc).Take(50).ToListAsync(ct);

        foreach (var m in pending)
        {
            _log.Append(new IntegrationEvent(m.Id, m.EventType, m.TenantId, m.OccurredAtUtc, m.Payload));
            m.DispatchedAtUtc = DateTime.UtcNow;
            m.Attempts++;
        }
        if (pending.Count > 0) { await db.SaveChangesAsync(ct); _logger.LogInformation("Dispatched {Count} event(s) to the local bus.", pending.Count); }
        return pending.Count;
    }
}

public sealed record SubscriberOptions(string Name);

/// <summary>Polls the local message-log for new events and invokes registered handlers idempotently,
/// advancing this service's offset (the stand-in for a Service Bus subscription consumer).</summary>
public sealed class LocalEventSubscriber : BackgroundService
{
    private readonly SqliteMessageLog _log;
    private readonly IServiceScopeFactory _scopes;
    private readonly SubscriberOptions _opts;
    private readonly ILogger<LocalEventSubscriber> _logger;

    public LocalEventSubscriber(SqliteMessageLog log, IServiceScopeFactory scopes, SubscriberOptions opts, ILogger<LocalEventSubscriber> logger)
    { _log = log; _scopes = scopes; _opts = opts; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await PumpOnceAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Event subscriber loop error for {Subscriber}.", _opts.Name); }
            await Task.Delay(500, ct);
        }
    }

    /// <summary>One pass: deliver all events past this subscriber's offset to handlers, then advance the
    /// offset. Handlers must be idempotent (offset only advances after a successful handle).</summary>
    public async Task<int> PumpOnceAsync(CancellationToken ct)
    {
        var offset = _log.GetOffset(_opts.Name);
        var batch = _log.ReadAfter(offset, 50);
        foreach (var (seq, evt) in batch)
        {
            using (var scope = _scopes.CreateScope())
                foreach (var h in scope.ServiceProvider.GetServices<IIntegrationEventHandler>())
                    await h.HandleAsync(evt, ct);
            _log.SetOffset(_opts.Name, seq);
        }
        return batch.Count;
    }
}
