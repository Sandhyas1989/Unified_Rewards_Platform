using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedRewards.Domain.Promotions;

namespace UnifiedRewards.Infrastructure.Persistence.Configurations;

public sealed class PromotionNominationConfiguration : IEntityTypeConfiguration<PromotionNomination>
{
    public void Configure(EntityTypeBuilder<PromotionNomination> builder)
    {
        builder.ToTable("PromotionNominations");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.CurrentGrade).HasConversion<int>();
        builder.Property(n => n.ProposedGrade).HasConversion<int>();
        builder.Property(n => n.Status).HasConversion<int>();
        builder.Property(n => n.Justification).IsRequired().HasMaxLength(2000);
        builder.Property(n => n.DecisionNotes).HasMaxLength(2000);

        builder.HasIndex(n => n.EmployeeId);
        builder.HasIndex(n => n.Status);
    }
}
