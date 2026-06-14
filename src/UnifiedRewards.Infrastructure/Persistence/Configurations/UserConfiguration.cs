using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedRewards.Domain.UserManagement;

namespace UnifiedRewards.Infrastructure.Persistence.Configurations;

/// <summary>
/// Table-Per-Hierarchy mapping for the User aggregate: one Users table with a
/// "UserType" discriminator distinguishing Employee / Manager / HrAdmin / FinanceUser.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Role).HasConversion<int>();

        builder.HasDiscriminator<string>("UserType")
            .HasValue<Employee>("Employee")
            .HasValue<Manager>("Manager")
            .HasValue<HrAdmin>("HrAdmin")
            .HasValue<FinanceUser>("FinanceUser");
    }
}
