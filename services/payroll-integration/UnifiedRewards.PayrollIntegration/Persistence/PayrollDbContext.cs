using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Messaging.Outbox;
using UnifiedRewards.PayrollIntegration.Domain;

namespace UnifiedRewards.PayrollIntegration.Persistence;

// This service OWNS its database (database-per-service).
public class PayrollDbContext : DbContext
{
    private readonly Guid? _tenantId;

    public PayrollDbContext(DbContextOptions<PayrollDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is not null)
        {
            var claim = http.User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(claim, out var t)) _tenantId = t;
        }
    }

    public DbSet<SettlementRequest> Settlements => Set<SettlementRequest>();
    public DbSet<Payslip> Payslips => Set<Payslip>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyOutbox();
        b.Entity<SettlementRequest>(e =>
        {
            e.ToTable("Settlements");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            // Store-generated rowversion on SQL Server only; plain column on SQLite/local (see SettlementRequest).
            if (Database.IsSqlServer()) e.Property(x => x.RowVersion).IsRowVersion();
            e.HasIndex(x => new { x.TenantId, x.EmployeeId });
            e.HasIndex(x => new { x.TenantId, x.ClaimId });
            e.HasQueryFilter(x => !_tenantId.HasValue || x.TenantId == _tenantId.GetValueOrDefault());
        });
        b.Entity<Payslip>(e =>
        {
            e.ToTable("Payslips");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.GrossMonthly).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalDeductionsMonthly).HasColumnType("decimal(18,2)");
            e.Property(x => x.NetMonthly).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Year, x.Month }).IsUnique();
            e.HasQueryFilter(x => !_tenantId.HasValue || x.TenantId == _tenantId.GetValueOrDefault());
        });
    }
}
