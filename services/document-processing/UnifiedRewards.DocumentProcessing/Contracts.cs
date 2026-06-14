namespace UnifiedRewards.DocumentProcessing;

public sealed record DocumentDto(
    Guid Id, Guid ClaimId, string FileName, string ContentType,
    string? OcrText, decimal? OcrConfidence, decimal? ExtractedAmount, DateTime UploadedAtUtc);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
