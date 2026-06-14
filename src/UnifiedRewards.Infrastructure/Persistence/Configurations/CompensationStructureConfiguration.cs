using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedRewards.Domain.Compensation;

namespace UnifiedRewards.Infrastructure.Persistence.Configurations;

public sealed class CompensationStructureConfiguration : IEntityTypeConfiguration<CompensationStructure>
{
    public void Configure(EntityTypeBuilder<CompensationStructure> builder)
    {
        builder.ToTable("CompensationStructures");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Grade).HasConversion<int>();
        builder.Property(s => s.Status).HasConversion<int>();
        builder.Property(s => s.AnnualBasic).HasColumnType("decimal(18,2)");
        builder.Property(s => s.GrossAnnual).HasColumnType("decimal(18,2)");
        builder.Property(s => s.TotalDeductions).HasColumnType("decimal(18,2)");
        builder.Property(s => s.NetAnnual).HasColumnType("decimal(18,2)");

        builder.HasIndex(s => s.EmployeeId);

        builder.HasMany(s => s.Components)
            .WithOne()
            .HasForeignKey(c => c.CompensationStructureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
