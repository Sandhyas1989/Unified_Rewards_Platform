using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedRewards.Domain.Reporting;

namespace UnifiedRewards.Infrastructure.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action).IsRequired().HasMaxLength(150);
        builder.Property(a => a.UserEmail).HasMaxLength(256);
        builder.Property(a => a.Error).HasMaxLength(2000);

        builder.HasIndex(a => a.OccurredAtUtc);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Action);
    }
}
