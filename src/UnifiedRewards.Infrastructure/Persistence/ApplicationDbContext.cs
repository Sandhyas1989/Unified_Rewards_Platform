using System.Reflection;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Domain.Benefits;
using UnifiedRewards.Domain.Claims;
using UnifiedRewards.Domain.Common;
using UnifiedRewards.Domain.Compensation;
using UnifiedRewards.Domain.Payroll;
using UnifiedRewards.Domain.Promotions;
using UnifiedRewards.Domain.Reporting;
using UnifiedRewards.Domain.UserManagement;

namespace UnifiedRewards.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<BenefitPlan> BenefitPlans => Set<BenefitPlan>();

    public DbSet<BenefitEnrollment> BenefitEnrollments => Set<BenefitEnrollment>();

    public DbSet<CompensationStructure> CompensationStructures => Set<CompensationStructure>();

    public DbSet<CompensationComponent> CompensationComponents => Set<CompensationComponent>();

    public DbSet<Claim> Claims => Set<Claim>();

    public DbSet<ClaimTransition> ClaimTransitions => Set<ClaimTransition>();

    public DbSet<Payslip> Payslips => Set<Payslip>();

    public DbSet<SettlementRequest> SettlementRequests => Set<SettlementRequest>();

    public DbSet<PromotionNomination> PromotionNominations => Set<PromotionNomination>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // All ids are client-generated (BaseEntity.Id = Guid.NewGuid()). Without this, EF's
        // convention treats a Guid key as store-generated and would mark a new child added to
        // an already-tracked aggregate as Modified (UPDATE) instead of Added (INSERT).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.Id))
                    .ValueGeneratedNever();
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
