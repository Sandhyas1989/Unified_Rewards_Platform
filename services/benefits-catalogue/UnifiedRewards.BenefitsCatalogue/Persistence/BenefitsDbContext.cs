using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.BenefitsCatalogue.Domain;

namespace UnifiedRewards.BenefitsCatalogue.Persistence;

public class BenefitsDbContext : DbContext
{
    private readonly Guid? _tenantId;

    public BenefitsDbContext(DbContextOptions<BenefitsDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is not null)
        {
            var claim = http.User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(claim, out var t)) _tenantId = t;
        }
        // null → background service scope; query filters are bypassed so dispatchers see all rows.
    }

    public DbSet<BenefitPlan> Plans => Set<BenefitPlan>();
    public DbSet<BenefitEnrollment> Enrollments => Set<BenefitEnrollment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<BenefitPlan>(e =>
        {
            e.ToTable("BenefitPlans");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Category).HasConversion<int>();
            e.Property(x => x.MonthlyCost).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            e.HasQueryFilter(x => !_tenantId.HasValue || x.TenantId == _tenantId.GetValueOrDefault());
        });
        b.Entity<BenefitEnrollment>(e =>
        {
            e.ToTable("BenefitEnrollments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Status).HasConversion<int>();
            e.HasIndex(x => new { x.TenantId, x.EmployeeId, x.BenefitPlanId });
            e.HasQueryFilter(x => !_tenantId.HasValue || x.TenantId == _tenantId.GetValueOrDefault());
        });
    }
}
