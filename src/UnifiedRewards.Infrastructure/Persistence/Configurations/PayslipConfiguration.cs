using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedRewards.Domain.Payroll;

namespace UnifiedRewards.Infrastructure.Persistence.Configurations;

public sealed class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
{
    public void Configure(EntityTypeBuilder<Payslip> builder)
    {
        builder.ToTable("Payslips");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.GrossMonthly).HasColumnType("decimal(18,2)");
        builder.Property(p => p.TotalDeductionsMonthly).HasColumnType("decimal(18,2)");
        builder.Property(p => p.NetMonthly).HasColumnType("decimal(18,2)");

        // One payslip per employee per period.
        builder.HasIndex(p => new { p.EmployeeId, p.Year, p.Month }).IsUnique();
    }
}
