using Microsoft.EntityFrameworkCore;
using UnifiedRewards.EmployeeProfile.Domain;

namespace UnifiedRewards.EmployeeProfile.Persistence;

// This service OWNS its database (database-per-service). TPH ported from the monolith.
public class EmployeeProfileDbContext : DbContext
{
    public EmployeeProfileDbContext(DbContextOptions<EmployeeProfileDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().ToTable("Users");
        b.Entity<User>().HasKey(u => u.Id);
        b.Entity<User>().Property(u => u.Id).ValueGeneratedNever();           // client-generated Guid
        b.Entity<User>().Property(u => u.Role).HasConversion<int>();
        b.Entity<User>().HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
        b.Entity<User>()
            .HasDiscriminator<string>("UserType")
            .HasValue<Employee>("Employee")
            .HasValue<Manager>("Manager")
            .HasValue<HrAdmin>("HrAdmin")
            .HasValue<FinanceUser>("Finance");
    }
}
