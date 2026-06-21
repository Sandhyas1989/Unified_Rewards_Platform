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
        var isCosmos = Database.IsCosmos();

        b.Entity<BenefitPlan>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Category).HasConversion<int>();
            if (isCosmos)
            {
                e.ToContainer("benefitPlans");
                e.HasPartitionKey(x => x.TenantId);
                e.HasNoDiscriminator();
                // No global query filter on Cosmos: the nullable-tenant OR predicate isn't translatable
                // to Cosmos SQL (it throws on every query, including the startup seeder). Each controller
                // query already filters by TenantId explicitly, so tenant isolation is preserved.
            }
            else
            {
                e.HasQueryFilter(x => !_tenantId.HasValue || x.TenantId == _tenantId.GetValueOrDefault());
                e.ToTable("BenefitPlans");
                e.Property(x => x.MonthlyCost).HasColumnType("decimal(18,2)");
                e.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            }
        });
        b.Entity<BenefitEnrollment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Status).HasConversion<int>();
            if (isCosmos)
            {
                e.ToContainer("benefitEnrollments");
                e.HasPartitionKey(x => x.TenantId);
                e.HasNoDiscriminator();
                // No global query filter on Cosmos (see BenefitPlan above); queries filter by TenantId explicitly.
            }
            else
            {
                e.HasQueryFilter(x => !_tenantId.HasValue || x.TenantId == _tenantId.GetValueOrDefault());
                e.ToTable("BenefitEnrollments");
                e.HasIndex(x => new { x.TenantId, x.EmployeeId, x.BenefitPlanId });
            }
        });
    }
}
