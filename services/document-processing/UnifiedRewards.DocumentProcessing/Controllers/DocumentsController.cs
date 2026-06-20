using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.DocumentProcessing.Domain;
using UnifiedRewards.DocumentProcessing.Ocr;
using UnifiedRewards.DocumentProcessing.Persistence;
using UnifiedRewards.DocumentProcessing.Storage;
using UnifiedRewards.Messaging;
using UnifiedRewards.Messaging.Events;

namespace UnifiedRewards.DocumentProcessing.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public sealed class DocumentsController : ControllerBase
{
    public const string TenantClaim = "tenant_id";

    private readonly DocumentDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IOcrEngine _ocr;
    private readonly IEventBus _bus;

    public DocumentsController(DocumentDbContext db, IFileStorage storage, IOcrEngine ocr, IEventBus bus)
    {
        _db = db;
        _storage = storage;
        _ocr = ocr;
        _bus = bus;
    }

    private Guid TenantId =>
        Guid.TryParse(User.FindFirst(TenantClaim)?.Value, out var t) ? t : Guid.Empty;

    private static DocumentDto ToDto(ReceiptDocument d) =>
        new(d.Id, d.ClaimId, d.FileName, d.ContentType, d.OcrText, d.OcrConfidence, d.ExtractedAmount, d.UploadedAtUtc);

    /// <summary>Uploads a receipt: stores the file (IFileStorage) and OCR-scans it (IOcrEngine).</summary>
    [HttpPost]
    public async Task<ActionResult<DocumentDto>> Upload([FromForm] UploadForm form, CancellationToken ct)
    {
        if (form.File is null || form.File.Length == 0)
        {
            return BadRequest(new { title = "A receipt file is required." });
        }

        using var ms = new MemoryStream();
        await form.File.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var reference = await _storage.UploadAsync(form.File.FileName, new MemoryStream(bytes), form.File.ContentType, ct);

        OcrResult? ocr = null;
        try { ocr = await _ocr.ExtractAsync(bytes, ct); } catch { /* OCR is best-effort */ }

        var doc = new ReceiptDocument
        {
            TenantId = TenantId,
            ClaimId = form.ClaimId,
            FileName = form.File.FileName,
            ContentType = form.File.ContentType,
            StorageReference = reference,
            OcrText = ocr?.Text,
            OcrConfidence = ocr?.Confidence,
            ExtractedAmount = ocr?.ExtractedAmount,
        };
        _db.Documents.Add(doc);

        // Tell the Reimbursement Workflow that this claim's receipt is processed (drives it to "In Review").
        // Staged in the same transaction as the document (outbox), then dispatched to the bus.
        if (doc.ClaimId != Guid.Empty)
        {
            await _bus.PublishAsync(
                new DocumentProcessed(doc.ClaimId, doc.Id, doc.ExtractedAmount, doc.OcrConfidence, DateTime.UtcNow),
                TenantId, ct);
        }

        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = doc.Id }, ToDto(doc));
    }

    /// <summary>Lists documents (paged, tenant-scoped), optionally filtered by claim.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<DocumentDto>>> Get(
        [FromQuery] Guid? claimId, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        var query = _db.Documents.AsNoTracking().Where(d => d.TenantId == TenantId);
        if (claimId is not null) query = query.Where(d => d.ClaimId == claimId);

        var p = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 25, 1, 200);
        var total = await query.CountAsync(ct);
        var docs = await query.OrderByDescending(d => d.UploadedAtUtc).Skip((p - 1) * size).Take(size).ToListAsync(ct);
        return Ok(new PagedResult<DocumentDto>(docs.Select(ToDto).ToList(), p, size, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id, CancellationToken ct)
    {
        var doc = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id && d.TenantId == TenantId, ct);
        return doc is null ? NotFound() : Ok(ToDto(doc));
    }

    /// <summary>Downloads the stored receipt file.</summary>
    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var doc = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id && d.TenantId == TenantId, ct);
        if (doc is null) return NotFound();
        var stream = await _storage.DownloadAsync(doc.StorageReference, ct);
        return File(stream, doc.ContentType, doc.FileName);
    }

    public sealed class UploadForm
    {
        public Guid ClaimId { get; set; }
        public IFormFile? File { get; set; }
    }
}
