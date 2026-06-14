using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedRewards.Domain.Compensation;

namespace UnifiedRewards.Infrastructure.Persistence.Configurations;

public sealed class CompensationComponentConfiguration : IEntityTypeConfiguration<CompensationComponent>
{
    public void Configure(EntityTypeBuilder<CompensationComponent> builder)
    {
        builder.ToTable("CompensationComponents");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Amount).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Type).HasConversion<int>();
    }
}
