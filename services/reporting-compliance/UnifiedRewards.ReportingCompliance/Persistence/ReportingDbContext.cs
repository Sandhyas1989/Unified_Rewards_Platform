using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.ReportingCompliance.Domain;

namespace UnifiedRewards.ReportingCompliance.Persistence;

public class ReportingDbContext : DbContext
{
    private readonly Guid? _tenantId;

    public ReportingDbContext(DbContextOptions<ReportingDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is not null)
        {
            var claim = http.User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(claim, out var t)) _tenantId = t;
        }
    }

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AuditEntry>(e =>
        {
            e.ToTable("AuditEntries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => x.EventId).IsUnique();                      // dedup on replay
            e.HasIndex(x => new { x.TenantId, x.ClaimId });
            e.HasIndex(x => new { x.TenantId, x.OccurredAtUtc });
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.EventType).HasMaxLength(100);
            e.HasQueryFilter(x => !_tenantId.HasValue || x.TenantId == _tenantId.GetValueOrDefault());
        });
    }
}
