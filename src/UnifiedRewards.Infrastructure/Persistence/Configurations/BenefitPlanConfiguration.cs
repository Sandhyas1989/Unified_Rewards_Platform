using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedRewards.Domain.Benefits;

namespace UnifiedRewards.Infrastructure.Persistence.Configurations;

public sealed class BenefitPlanConfiguration : IEntityTypeConfiguration<BenefitPlan>
{
    public void Configure(EntityTypeBuilder<BenefitPlan> builder)
    {
        builder.ToTable("BenefitPlans");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.HasIndex(p => p.Name).IsUnique();
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Category).HasConversion<int>();
        builder.Property(p => p.MonthlyCost).HasColumnType("decimal(18,2)");

        builder.HasMany(p => p.Enrollments)
            .WithOne(e => e.BenefitPlan!)
            .HasForeignKey(e => e.BenefitPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
