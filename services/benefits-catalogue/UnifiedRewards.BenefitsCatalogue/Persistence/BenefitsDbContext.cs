using Microsoft.EntityFrameworkCore;
using UnifiedRewards.BenefitsCatalogue.Domain;

namespace UnifiedRewards.BenefitsCatalogue.Persistence;

public class BenefitsDbContext : DbContext
{
    public BenefitsDbContext(DbContextOptions<BenefitsDbContext> options) : base(options) { }

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
        });
        b.Entity<BenefitEnrollment>(e =>
        {
            e.ToTable("BenefitEnrollments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Status).HasConversion<int>();
            e.HasIndex(x => new { x.TenantId, x.EmployeeId, x.BenefitPlanId });
        });
    }
}
