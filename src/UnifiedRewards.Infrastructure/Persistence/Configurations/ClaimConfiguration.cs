using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedRewards.Domain.Claims;

namespace UnifiedRewards.Infrastructure.Persistence.Configurations;

public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Type).HasConversion<int>();
        builder.Property(c => c.Status).HasConversion<int>();
        builder.Property(c => c.Amount).HasColumnType("decimal(18,2)");
        builder.Property(c => c.OcrConfidence).HasColumnType("decimal(5,4)");
        builder.Property(c => c.OcrExtractedAmount).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Description).IsRequired().HasMaxLength(1000);
        builder.Property(c => c.ReceiptFileName).HasMaxLength(260);
        builder.Property(c => c.ReceiptContentType).HasMaxLength(150);

        builder.HasIndex(c => c.EmployeeId);
        builder.HasIndex(c => c.Status);

        builder.HasMany(c => c.History)
            .WithOne()
            .HasForeignKey(h => h.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
