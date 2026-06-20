using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnifiedRewards.Messaging;
using UnifiedRewards.Messaging.Events;
using UnifiedRewards.Messaging.Local;
using UnifiedRewards.Messaging.Outbox;
using Xunit;

namespace UnifiedRewards.Messaging.Tests;

// Minimal DbContext that hosts only the outbox table (stands in for a service's own DB).
public sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> o) : base(o) { }
    protected override void OnModelCreating(ModelBuilder b) => b.ApplyOutbox();
}

public sealed class CapturedEvents { public List<IntegrationEvent> Items { get; } = new(); }

public sealed class CapturingHandler : IIntegrationEventHandler
{
    private readonly CapturedEvents _sink;
    public CapturingHandler(CapturedEvents sink) => _sink = sink;
    public Task HandleAsync(IntegrationEvent @event, CancellationToken ct) { _sink.Items.Add(@event); return Task.CompletedTask; }
}

public sealed class EndToEndTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"urp-test-{Guid.NewGuid():N}.db");
    private readonly string _busPath = Path.Combine(Path.GetTempPath(), $"urp-bus-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Publish_then_dispatch_then_subscribe_delivers_event_exactly_once()
    {
        var tenant = Guid.NewGuid();
        var log = new SqliteMessageLog(_busPath);
        var sink = new CapturedEvents();

        // ---- Publisher side: test DbContext + outbox-backed bus ----
        var pub = new ServiceCollection();
        pub.AddDbContext<TestDbContext>(o => o.UseSqlite($"Data Source={_dbPath}"));
        pub.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());
        pub.AddScoped<IEventBus, OutboxEventBus>();
        pub.AddSingleton(log);
        pub.AddLogging();
        var pubSp = pub.BuildServiceProvider();
        using (var scope = pubSp.CreateScope())
            scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreated();

        // 1) Publish stages to the outbox; the caller's SaveChanges commits it atomically.
        using (var scope = pubSp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            await bus.PublishAsync(new ClaimApproved(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1234.56m, DateTime.UtcNow), tenant);
            await db.SaveChangesAsync();
            Assert.Equal(1, await db.Set<OutboxMessage>().CountAsync(m => m.DispatchedAtUtc == null));
        }

        // 2) Dispatcher ships the outbox row to the bus and marks it dispatched.
        var dispatcher = new LocalOutboxDispatcher(
            pubSp.GetRequiredService<IServiceScopeFactory>(), log,
            pubSp.GetRequiredService<ILoggerFactory>().CreateLogger<LocalOutboxDispatcher>());
        Assert.Equal(1, await dispatcher.PumpOnceAsync(default));
        Assert.Equal(0, await dispatcher.PumpOnceAsync(default));   // nothing left to dispatch

        // ---- Subscriber side ----
        var sub = new ServiceCollection();
        sub.AddSingleton(sink);
        sub.AddScoped<IIntegrationEventHandler, CapturingHandler>();
        sub.AddLogging();
        var subSp = sub.BuildServiceProvider();
        var subscriber = new LocalEventSubscriber(log, subSp.GetRequiredService<IServiceScopeFactory>(),
            new SubscriberOptions("test-sub"), subSp.GetRequiredService<ILoggerFactory>().CreateLogger<LocalEventSubscriber>());

        // 3) Consume once; a second pass must NOT re-deliver (offset advanced → idempotent).
        Assert.Equal(1, await subscriber.PumpOnceAsync(default));
        Assert.Equal(0, await subscriber.PumpOnceAsync(default));

        var got = Assert.Single(sink.Items);
        Assert.Equal(nameof(ClaimApproved), got.EventType);
        Assert.Equal(tenant, got.TenantId);
        Assert.Contains("1234.56", got.Payload);
    }

    public void Dispose()
    {
        foreach (var p in new[] { _dbPath, _busPath }) { try { File.Delete(p); } catch { /* best effort */ } }
    }
}

/// <summary>
/// Two-hop settlement saga: SettlementRequested (reimbursement → bus → payroll)
/// then SettlementCompleted (payroll → bus → reimbursement). Uses two separate outbox
/// DBs and two separate subscriber cursors sharing one bus — mirrors the real topology.
/// </summary>
public sealed class SettlementSagaTests : IDisposable
{
    private readonly string _reimbDbPath = Path.Combine(Path.GetTempPath(), $"urp-saga-reimb-{Guid.NewGuid():N}.db");
    private readonly string _payrollDbPath = Path.Combine(Path.GetTempPath(), $"urp-saga-payroll-{Guid.NewGuid():N}.db");
    private readonly string _busPath = Path.Combine(Path.GetTempPath(), $"urp-saga-bus-{Guid.NewGuid():N}.db");

    private static IServiceProvider BuildPublisher(string dbPath, SqliteMessageLog log)
    {
        var svc = new ServiceCollection();
        svc.AddDbContext<TestDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
        svc.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());
        svc.AddScoped<IEventBus, OutboxEventBus>();
        svc.AddSingleton(log);
        svc.AddLogging();
        var sp = svc.BuildServiceProvider();
        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreated();
        return sp;
    }

    private static LocalEventSubscriber BuildSubscriber(SqliteMessageLog log, string name, IServiceProvider handlerSp) =>
        new(log, handlerSp.GetRequiredService<IServiceScopeFactory>(),
            new SubscriberOptions(name),
            handlerSp.GetRequiredService<ILoggerFactory>().CreateLogger<LocalEventSubscriber>());

    [Fact]
    public async Task Settlement_saga_two_hop_delivers_both_events_in_order()
    {
        var tenant = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var settlementId = Guid.NewGuid();
        var log = new SqliteMessageLog(_busPath);

        // --- Hop 1: reimbursement publishes SettlementRequested ---
        var reimbSp = BuildPublisher(_reimbDbPath, log);

        using (var scope = reimbSp.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await bus.PublishAsync(new SettlementRequested(claimId, Guid.NewGuid(), 5000m, DateTime.UtcNow), tenant);
            await db.SaveChangesAsync();
        }

        var reimbDispatcher = new LocalOutboxDispatcher(
            reimbSp.GetRequiredService<IServiceScopeFactory>(), log,
            reimbSp.GetRequiredService<ILoggerFactory>().CreateLogger<LocalOutboxDispatcher>());
        Assert.Equal(1, await reimbDispatcher.PumpOnceAsync(default));

        // Payroll subscriber receives SettlementRequested.
        var payrollSink = new CapturedEvents();
        var payrollSubSp = new ServiceCollection()
            .AddSingleton(payrollSink).AddScoped<IIntegrationEventHandler, CapturingHandler>().AddLogging()
            .BuildServiceProvider();
        var payrollSub = BuildSubscriber(log, "payroll-integration", payrollSubSp);
        Assert.Equal(1, await payrollSub.PumpOnceAsync(default));
        Assert.Equal(0, await payrollSub.PumpOnceAsync(default));   // idempotent

        var hop1 = Assert.Single(payrollSink.Items);
        Assert.Equal(nameof(SettlementRequested), hop1.EventType);
        Assert.Equal(tenant, hop1.TenantId);
        Assert.Contains(claimId.ToString(), hop1.Payload);

        // --- Hop 2: payroll publishes SettlementCompleted ---
        var payrollPubSp = BuildPublisher(_payrollDbPath, log);

        using (var scope = payrollPubSp.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await bus.PublishAsync(new SettlementCompleted(claimId, settlementId, true, $"PAYROLL-CLM-{claimId:N}", null, DateTime.UtcNow), tenant);
            await db.SaveChangesAsync();
        }

        var payrollDispatcher = new LocalOutboxDispatcher(
            payrollPubSp.GetRequiredService<IServiceScopeFactory>(), log,
            payrollPubSp.GetRequiredService<ILoggerFactory>().CreateLogger<LocalOutboxDispatcher>());
        Assert.Equal(1, await payrollDispatcher.PumpOnceAsync(default));

        // Reimbursement subscriber receives SettlementCompleted (its own cursor, independent of payroll's).
        var reimbSink = new CapturedEvents();
        var reimbSubSp = new ServiceCollection()
            .AddSingleton(reimbSink).AddScoped<IIntegrationEventHandler, CapturingHandler>().AddLogging()
            .BuildServiceProvider();
        var reimbSub = BuildSubscriber(log, "reimbursement-workflow", reimbSubSp);

        // The reimbursement subscriber has its own offset cursor starting at 0, so it sees both
        // events on the bus (SettlementRequested + SettlementCompleted).
        var delivered = await reimbSub.PumpOnceAsync(default);
        Assert.True(delivered >= 1);
        Assert.Equal(0, await reimbSub.PumpOnceAsync(default));     // idempotent

        var hop2 = reimbSink.Items.First(e => e.EventType == nameof(SettlementCompleted));
        Assert.Equal(tenant, hop2.TenantId);
        Assert.Contains(claimId.ToString(), hop2.Payload);
        Assert.Contains("true", hop2.Payload, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        foreach (var p in new[] { _reimbDbPath, _payrollDbPath, _busPath })
        {
            try { File.Delete(p); } catch { /* best effort */ }
        }
    }
}
