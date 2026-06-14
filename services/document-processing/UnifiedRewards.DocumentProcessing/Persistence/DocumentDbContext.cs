using Microsoft.EntityFrameworkCore;
using UnifiedRewards.DocumentProcessing.Domain;

namespace UnifiedRewards.DocumentProcessing.Persistence;

// This service OWNS its database (database-per-service).
public class DocumentDbContext : DbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options) : base(options) { }

    public DbSet<ReceiptDocument> Documents => Set<ReceiptDocument>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ReceiptDocument>().ToTable("Documents");
        b.Entity<ReceiptDocument>().HasKey(d => d.Id);
        b.Entity<ReceiptDocument>().Property(d => d.Id).ValueGeneratedNever();
        b.Entity<ReceiptDocument>().Property(d => d.OcrConfidence).HasColumnType("decimal(5,4)");
        b.Entity<ReceiptDocument>().Property(d => d.ExtractedAmount).HasColumnType("decimal(18,2)");
        b.Entity<ReceiptDocument>().HasIndex(d => new { d.TenantId, d.ClaimId });
    }
}
