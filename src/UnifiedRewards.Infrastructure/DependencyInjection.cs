using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Common.Interfaces.Integration;
using UnifiedRewards.Infrastructure.Compensation;
using UnifiedRewards.Infrastructure.Identity;
using UnifiedRewards.Infrastructure.Integration;
using UnifiedRewards.Infrastructure.Ocr;
using UnifiedRewards.Infrastructure.Payroll;
using UnifiedRewards.Infrastructure.Persistence;
using UnifiedRewards.Infrastructure.Reporting;

namespace UnifiedRewards.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database provider: SQL Server in production (config Database:Provider=SqlServer), else SQLite
        // for zero-install local development. Business logic is unaffected by the choice.
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=unifiedrewards.db";
        var useSqlServer = string.Equals(configuration["Database:Provider"], "SqlServer", StringComparison.OrdinalIgnoreCase);
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (useSqlServer)
            {
                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        var jwtSettings = new JwtSettings();
        configuration.GetSection("Jwt").Bind(jwtSettings);
        services.AddSingleton(jwtSettings);

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        // Compensation rules engine (NRules) — compiled once, reused across requests.
        services.AddSingleton<ICompensationCalculator, NRulesCompensationCalculator>();

        // Reporting: ClosedXML-based Excel export.
        services.AddSingleton<IReportExporter, ClosedXmlReportExporter>();

        // Integration seams — local-dev implementations by default.
        services.AddScoped<IEmailService, LocalEmailService>();
        services.AddScoped<IEventBus, MediatrEventBus>();

        // File storage: Azure Blob in production (Storage:Provider=Blob), else local file system.
        if (string.Equals(configuration["Storage:Provider"], "Blob", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IFileStorage>(sp => new AzureBlobFileStorage(configuration));
        }
        else
        {
            services.AddSingleton<IFileStorage, LocalFileStorage>();
        }

        // Payroll: the external system wrapped by a Polly resilience pipeline (retry/breaker/timeout).
        services.AddSingleton(sp =>
            PayrollResilience.BuildPipeline(sp.GetRequiredService<ILoggerFactory>().CreateLogger("Payroll.Resilience")));
        services.AddScoped<MockPayrollService>();
        services.AddScoped<IPayrollService, ResilientPayrollService>();

        // Asynchronous settlement processing (shared logic, then a provider-specific transport).
        services.AddSingleton<SettlementProcessor>();
        if (string.Equals(configuration["Messaging:Provider"], "ServiceBus", StringComparison.OrdinalIgnoreCase))
        {
            // Production: durable Azure Service Bus queue + competing-consumer worker.
            var sbConnection = configuration["Messaging:ServiceBus:ConnectionString"]
                ?? throw new InvalidOperationException("Messaging:ServiceBus:ConnectionString is required when Messaging:Provider=ServiceBus.");
            var queueName = configuration["Messaging:ServiceBus:QueueName"] ?? "settlements";
            services.AddSingleton(new Azure.Messaging.ServiceBus.ServiceBusClient(sbConnection));
            services.AddSingleton<ISettlementQueue>(sp =>
                new ServiceBusSettlementQueue(sp.GetRequiredService<Azure.Messaging.ServiceBus.ServiceBusClient>(), queueName));
            services.AddHostedService(sp => new ServiceBusSettlementConsumer(
                sp.GetRequiredService<Azure.Messaging.ServiceBus.ServiceBusClient>(), queueName,
                sp.GetRequiredService<SettlementProcessor>(),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ServiceBusSettlementConsumer>()));
        }
        else
        {
            // Local/default: in-process channel queue + background worker.
            services.AddSingleton<SettlementChannelQueue>();
            services.AddSingleton<ISettlementQueue>(sp => sp.GetRequiredService<SettlementChannelQueue>());
            services.AddHostedService<SettlementBackgroundService>();
        }

        // OCR engine: Tesseract (needs native libs + tessdata) when configured, else the zero-install stub.
        if (string.Equals(configuration["Ocr:Engine"], "Tesseract", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IOcrEngine, TesseractOcrEngine>();
        }
        else
        {
            services.AddScoped<IOcrEngine, StubOcrEngine>();
        }

        return services;
    }
}
