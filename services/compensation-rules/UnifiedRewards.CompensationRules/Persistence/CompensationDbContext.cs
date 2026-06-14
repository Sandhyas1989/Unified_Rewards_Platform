using Microsoft.EntityFrameworkCore;
using UnifiedRewards.CompensationRules.Domain;

namespace UnifiedRewards.CompensationRules.Persistence;

public class CompensationDbContext : DbContext
{
    public CompensationDbContext(DbContextOptions<CompensationDbContext> options) : base(options) { }

    public DbSet<CompensationStructure> Structures => Set<CompensationStructure>();
    public DbSet<CompensationComponent> Components => Set<CompensationComponent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<CompensationStructure>(e =>
        {
            e.ToTable("CompensationStructures");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Grade).HasConversion<int>();
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.AnnualBasic).HasColumnType("decimal(18,2)");
            e.Property(x => x.GrossAnnual).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalDeductions).HasColumnType("decimal(18,2)");
            e.Property(x => x.NetAnnual).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.TenantId, x.EmployeeId });
            e.HasMany(x => x.Components).WithOne().HasForeignKey(c => c.CompensationStructureId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<CompensationComponent>(e =>
        {
            e.ToTable("CompensationComponents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Type).HasConversion<int>();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        });
    }
}
