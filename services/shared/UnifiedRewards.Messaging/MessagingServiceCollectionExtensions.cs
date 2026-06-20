using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UnifiedRewards.Messaging.Azure;
using UnifiedRewards.Messaging.Local;
using UnifiedRewards.Messaging.Outbox;

namespace UnifiedRewards.Messaging;

public static class MessagingServiceCollectionExtensions
{
    private static bool UseServiceBus(IConfiguration config) =>
        string.Equals(config["Messaging:Provider"], "ServiceBus", StringComparison.OrdinalIgnoreCase);

    private static string LocalBusPath(IConfiguration config) =>
        config["Messaging:Local:BusPath"] ?? Path.Combine(Path.GetTempPath(), "unifiedrewards", "bus.db");

    private static ServiceBusOptions ServiceBusOpts(IConfiguration config) => new(
        config["Messaging:ServiceBus:ConnectionString"] ?? throw new InvalidOperationException("Messaging:ServiceBus:ConnectionString is required."),
        config["Messaging:ServiceBus:Topic"] ?? "urp-events",
        config["Messaging:ServiceBus:Subscription"]);

    /// <summary>For services that PUBLISH events. Registers the outbox-backed IEventBus and the dispatcher.
    /// The outbox lives in <typeparamref name="TDbContext"/> (call ApplyOutbox() in its OnModelCreating).</summary>
    public static IServiceCollection AddEventPublishing<TDbContext>(this IServiceCollection services, IConfiguration config)
        where TDbContext : DbContext
    {
        // Bus + dispatcher read the SERVICE's own DbContext via the DbContext base type.
        services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());
        services.AddScoped<IEventBus, OutboxEventBus>();

        if (UseServiceBus(config))
        {
            services.TryAddSingleton(ServiceBusOpts(config));
            services.AddHostedService<ServiceBusOutboxDispatcher>();
        }
        else
        {
            services.TryAddSingleton(new SqliteMessageLog(LocalBusPath(config)));
            services.AddHostedService<LocalOutboxDispatcher>();
        }
        return services;
    }

    /// <summary>For services that CONSUME events. Registers the subscriber for this service. Pair with
    /// one or more AddEventHandler&lt;T&gt;() calls.</summary>
    public static IServiceCollection AddEventSubscribing(this IServiceCollection services, IConfiguration config, string subscriberName)
    {
        if (UseServiceBus(config))
        {
            services.TryAddSingleton(ServiceBusOpts(config) with { Subscription = config["Messaging:ServiceBus:Subscription"] ?? subscriberName });
            services.AddHostedService<ServiceBusSubscriber>();
        }
        else
        {
            services.TryAddSingleton(new SqliteMessageLog(LocalBusPath(config)));
            services.TryAddSingleton(new SubscriberOptions(subscriberName));
            services.AddHostedService<LocalEventSubscriber>();
        }
        return services;
    }

    public static IServiceCollection AddEventHandler<THandler>(this IServiceCollection services)
        where THandler : class, IIntegrationEventHandler
    {
        services.AddScoped<IIntegrationEventHandler, THandler>();
        return services;
    }
}
