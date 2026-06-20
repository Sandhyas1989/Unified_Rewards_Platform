using Microsoft.EntityFrameworkCore;
using UnifiedRewards.ReimbursementWorkflow.Domain;
using UnifiedRewards.ReimbursementWorkflow.Persistence;

namespace UnifiedRewards.ReimbursementWorkflow;

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
        var terminal = new[] { ClaimStatus.Settled, ClaimStatus.Rejected };
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ReimbursementDbContext>();
            // Delete transitions first (SQLite doesn't auto-cascade without PRAGMA foreign_keys=ON).
            var oldClaimIds = await db.Claims
                .Where(c => terminal.Contains(c.Status) && c.SubmittedAtUtc < cutoff)
                .Select(c => c.Id)
                .ToListAsync(ct);
            if (oldClaimIds.Count == 0) return;
            await db.ClaimTransitions
                .Where(t => oldClaimIds.Contains(t.ClaimId))
                .ExecuteDeleteAsync(ct);
            var deleted = await db.Claims
                .Where(c => terminal.Contains(c.Status) && c.SubmittedAtUtc < cutoff)
                .ExecuteDeleteAsync(ct);
            _logger.LogInformation("Retention: purged {Count} terminal claims older than {Years} years", deleted, _yearsToKeep);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Retention purge failed");
        }
    }
}
