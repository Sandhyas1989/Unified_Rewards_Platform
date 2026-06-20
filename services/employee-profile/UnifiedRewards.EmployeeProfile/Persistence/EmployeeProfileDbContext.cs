using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.EmployeeProfile.Domain;
using UnifiedRewards.Messaging.Outbox;

namespace UnifiedRewards.EmployeeProfile.Persistence;

// This service OWNS its database (database-per-service). TPH ported from the monolith.
public class EmployeeProfileDbContext : DbContext
{
    private readonly Guid? _tenantId;

    public EmployeeProfileDbContext(DbContextOptions<EmployeeProfileDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is not null)
        {
            var claim = http.User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(claim, out var t)) _tenantId = t;
        }
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionEligibility> PromotionEligibilities => Set<PromotionEligibility>();
    public DbSet<PromotionNomination> PromotionNominations => Set<PromotionNomination>();
    public DbSet<PromotionEligibilityCheck> PromotionEligibilityChecks => Set<PromotionEligibilityCheck>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyOutbox();
        b.Entity<User>().ToTable("Users");
        b.Entity<User>().HasKey(u => u.Id);
        b.Entity<User>().Property(u => u.Id).ValueGeneratedNever();
        b.Entity<User>().Property(u => u.Role).HasConversion<int>();
        b.Entity<User>().HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
        b.Entity<User>().HasQueryFilter(u => !_tenantId.HasValue || u.TenantId == _tenantId.GetValueOrDefault());
        b.Entity<User>()
            .HasDiscriminator<string>("UserType")
            .HasValue<Employee>("Employee")
            .HasValue<Manager>("Manager")
            .HasValue<HrAdmin>("HrAdmin")
            .HasValue<FinanceUser>("Finance");

        b.Entity<Promotion>(e =>
        {
            e.ToTable("Promotions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.Title).HasMaxLength(150);
            e.Property(x => x.CycleQuarter).HasMaxLength(2);
            e.Property(x => x.FromGrade).HasMaxLength(10);
            e.Property(x => x.BonusValue).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.TenantId, x.CycleYear, x.CycleQuarter });
            e.HasOne(x => x.Eligibility).WithOne().HasForeignKey<PromotionEligibility>(x => x.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Nominations).WithOne().HasForeignKey(x => x.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !_tenantId.HasValue || x.TenantId == _tenantId.GetValueOrDefault());
        });

        b.Entity<PromotionEligibility>(e =>
        {
            e.ToTable("PromotionEligibilities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => x.PromotionId).IsUnique();   // one eligibility per cycle
            e.Property(x => x.MinPerformanceRating).HasMaxLength(5);
            e.Property(x => x.MinCurrentGrade).HasMaxLength(10);
        });

        b.Entity<PromotionNomination>(e =>
        {
            e.ToTable("PromotionNominations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Outcome).HasConversion<int>();
            e.Property(x => x.Remarks).HasMaxLength(500);
            e.HasIndex(x => new { x.PromotionId, x.EmployeeId }).IsUnique();   // no duplicate nominations
            e.HasIndex(x => new { x.TenantId, x.EmployeeId });
            e.HasQueryFilter(x => !_tenantId.HasValue || x.TenantId == _tenantId.GetValueOrDefault());
        });

        b.Entity<PromotionEligibilityCheck>(e =>
        {
            e.ToTable("PromotionEligibilityChecks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.PerformanceRating).HasMaxLength(5);
            e.Property(x => x.FailureReason).HasMaxLength(300);
            e.HasIndex(x => new { x.PromotionId, x.EmployeeId });
        });
    }
}
