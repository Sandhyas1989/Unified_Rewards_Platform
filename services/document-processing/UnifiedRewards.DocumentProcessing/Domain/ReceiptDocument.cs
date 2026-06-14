namespace UnifiedRewards.DocumentProcessing.Domain;

// A stored receipt/document + its OCR results. Owned solely by this service.
public class ReceiptDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ClaimId { get; set; }                 // reference (not a FK) to the Reimbursement service
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StorageReference { get; set; } = string.Empty;  // returned by IFileStorage
    public string? OcrText { get; set; }
    public decimal? OcrConfidence { get; set; }
    public decimal? ExtractedAmount { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
