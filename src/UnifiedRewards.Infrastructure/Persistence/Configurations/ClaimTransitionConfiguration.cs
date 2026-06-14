using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedRewards.Domain.Claims;

namespace UnifiedRewards.Infrastructure.Persistence.Configurations;

public sealed class ClaimTransitionConfiguration : IEntityTypeConfiguration<ClaimTransition>
{
    public void Configure(EntityTypeBuilder<ClaimTransition> builder)
    {
        builder.ToTable("ClaimTransitions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.FromStatus).HasConversion<int?>();
        builder.Property(t => t.ToStatus).HasConversion<int>();
        builder.Property(t => t.Notes).HasMaxLength(1000);

        builder.HasIndex(t => t.ClaimId);
    }
}
