using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.DocumentProcessing.Domain;
using UnifiedRewards.Messaging.Outbox;

namespace UnifiedRewards.DocumentProcessing.Persistence;

// This service OWNS its database (database-per-service).
public class DocumentDbContext : DbContext
{
    private readonly Guid? _tenantId;

    public DocumentDbContext(DbContextOptions<DocumentDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is not null)
        {
            var claim = http.User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(claim, out var t)) _tenantId = t;
        }
    }

    public DbSet<ReceiptDocument> Documents => Set<ReceiptDocument>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyOutbox();   // transactional outbox in this service's own DB
        b.Entity<ReceiptDocument>().ToTable("Documents");
        b.Entity<ReceiptDocument>().HasKey(d => d.Id);
        b.Entity<ReceiptDocument>().Property(d => d.Id).ValueGeneratedNever();
        b.Entity<ReceiptDocument>().Property(d => d.OcrConfidence).HasColumnType("decimal(5,4)");
        b.Entity<ReceiptDocument>().Property(d => d.ExtractedAmount).HasColumnType("decimal(18,2)");
        b.Entity<ReceiptDocument>().HasIndex(d => new { d.TenantId, d.ClaimId });
        b.Entity<ReceiptDocument>().HasQueryFilter(d => !_tenantId.HasValue || d.TenantId == _tenantId.GetValueOrDefault());
    }
}
