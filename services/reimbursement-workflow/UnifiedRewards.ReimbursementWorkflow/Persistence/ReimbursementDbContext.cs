using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Messaging.Outbox;
using UnifiedRewards.ReimbursementWorkflow.Domain;

namespace UnifiedRewards.ReimbursementWorkflow.Persistence;

public class ReimbursementDbContext : DbContext
{
    private readonly Guid? _tenantId;

    public ReimbursementDbContext(DbContextOptions<ReimbursementDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is not null)
        {
            var claim = http.User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(claim, out var t)) _tenantId = t;
        }
    }

    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimTransition> ClaimTransitions => Set<ClaimTransition>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyOutbox();   // transactional outbox table lives in this service's own DB
        b.Entity<Claim>(e =>
        {
            e.ToTable("Claims");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Type).HasConversion<int>();
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.TenantId, x.EmployeeId });
            e.HasIndex(x => new { x.TenantId, x.Status });
            e.HasMany(x => x.History).WithOne().HasForeignKey(h => h.ClaimId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !_tenantId.HasValue || x.TenantId == _tenantId.GetValueOrDefault());
        });
        b.Entity<ClaimTransition>(e =>
        {
            e.ToTable("ClaimTransitions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.FromStatus).HasConversion<int?>();
            e.Property(x => x.ToStatus).HasConversion<int>();
        });
    }
}
