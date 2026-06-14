using Microsoft.EntityFrameworkCore;
using UnifiedRewards.PayrollIntegration.Domain;

namespace UnifiedRewards.PayrollIntegration.Persistence;

// This service OWNS its database (database-per-service).
public class PayrollDbContext : DbContext
{
    public PayrollDbContext(DbContextOptions<PayrollDbContext> options) : base(options) { }

    public DbSet<SettlementRequest> Settlements => Set<SettlementRequest>();
    public DbSet<Payslip> Payslips => Set<Payslip>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<SettlementRequest>(e =>
        {
            e.ToTable("Settlements");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.TenantId, x.EmployeeId });
        });
        b.Entity<Payslip>(e =>
        {
            e.ToTable("Payslips");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.GrossMonthly).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalDeductionsMonthly).HasColumnType("decimal(18,2)");
            e.Property(x => x.NetMonthly).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Year, x.Month }).IsUnique();
        });
    }
}
