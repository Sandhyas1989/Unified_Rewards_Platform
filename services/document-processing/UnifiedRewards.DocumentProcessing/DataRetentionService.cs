using Microsoft.EntityFrameworkCore;
using UnifiedRewards.DocumentProcessing.Persistence;

namespace UnifiedRewards.DocumentProcessing;

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
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
            var deleted = await db.Documents
                .Where(d => d.UploadedAtUtc < cutoff)
                .ExecuteDeleteAsync(ct);
            if (deleted > 0)
                _logger.LogInformation("Retention: purged {Count} receipt documents older than {Years} years", deleted, _yearsToKeep);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Retention purge failed");
        }
    }
}
