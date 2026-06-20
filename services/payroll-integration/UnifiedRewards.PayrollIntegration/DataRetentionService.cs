using Microsoft.EntityFrameworkCore;
using UnifiedRewards.PayrollIntegration.Domain;
using UnifiedRewards.PayrollIntegration.Persistence;

namespace UnifiedRewards.PayrollIntegration;

public sealed class DataRetentionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataRetentionService> _logger;
    private readonly int _yearsToKeep;

    public DataRetentionService(IServiceScopeFactory scopeFactory, ILogger<DataRetentionService> logger, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _yearsToKeep = config.GetValue<int>("Retention:YearsToKeep", 7);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await PurgeAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task PurgeAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddYears(-_yearsToKeep);
        var terminal = new[] { SettlementStatus.Succeeded, SettlementStatus.Failed };
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PayrollDbContext>();
            var deleted = await db.Settlements
                .Where(s => terminal.Contains(s.Status) && s.RequestedAtUtc < cutoff)
                .ExecuteDeleteAsync(ct);
            if (deleted > 0)
                _logger.LogInformation("Retention: purged {Count} completed settlements older than {Years} years", deleted, _yearsToKeep);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Retention purge failed");
        }
    }
}
