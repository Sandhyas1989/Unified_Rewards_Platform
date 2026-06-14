using Microsoft.EntityFrameworkCore;
using UnifiedRewards.ReimbursementWorkflow.Domain;

namespace UnifiedRewards.ReimbursementWorkflow.Persistence;

public class ReimbursementDbContext : DbContext
{
    public ReimbursementDbContext(DbContextOptions<ReimbursementDbContext> options) : base(options) { }

    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimTransition> ClaimTransitions => Set<ClaimTransition>();

    protected override void OnModelCreating(ModelBuilder b)
    {
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
