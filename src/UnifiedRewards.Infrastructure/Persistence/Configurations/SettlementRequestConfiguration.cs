using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedRewards.Domain.Payroll;

namespace UnifiedRewards.Infrastructure.Persistence.Configurations;

public sealed class SettlementRequestConfiguration : IEntityTypeConfiguration<SettlementRequest>
{
    public void Configure(EntityTypeBuilder<SettlementRequest> builder)
    {
        builder.ToTable("SettlementRequests");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Amount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.Status).HasConversion<int>();
        builder.Property(s => s.Reference).IsRequired().HasMaxLength(80);
        builder.Property(s => s.PayrollConfirmation).HasMaxLength(120);
        builder.Property(s => s.LastError).HasMaxLength(1000);

        builder.HasIndex(s => s.Reference).IsUnique();
        builder.HasIndex(s => s.EmployeeId);
    }
}
