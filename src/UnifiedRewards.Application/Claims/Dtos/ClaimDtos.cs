using UnifiedRewards.Domain.Claims;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Claims.Dtos;

public sealed record ClaimTransitionDto(
    ClaimStatus? FromStatus,
    ClaimStatus ToStatus,
    Guid ActorId,
    string? Notes,
    DateTime OccurredAtUtc);

public sealed record ClaimDto(
    Guid Id,
    Guid EmployeeId,
    ClaimType Type,
    decimal Amount,
    string Description,
    ClaimStatus Status,
    bool HasReceipt,
    string? ReceiptFileName,
    string? OcrText,
    decimal? OcrConfidence,
    decimal? OcrExtractedAmount,
    Guid? ReviewerId,
    string? DecisionNotes,
    DateTime SubmittedAtUtc,
    DateTime? DecisionAtUtc,
    DateTime? SettledAtUtc,
    string? PayrollReference,
    IReadOnlyList<ClaimTransitionDto> History);

/// <summary>Streamed receipt content returned by the download query.</summary>
public sealed record ReceiptFile(Stream Content, string FileName, string ContentType);

public static class ClaimMapping
{
    public static ClaimDto ToDto(this Claim c) => new(
        c.Id,
        c.EmployeeId,
        c.Type,
        c.Amount,
        c.Description,
        c.Status,
        c.ReceiptReference is not null,
        c.ReceiptFileName,
        c.OcrText,
        c.OcrConfidence,
        c.OcrExtractedAmount,
        c.ReviewerId,
        c.DecisionNotes,
        c.SubmittedAtUtc,
        c.DecisionAtUtc,
        c.SettledAtUtc,
        c.PayrollReference,
        c.History
            .OrderBy(h => h.OccurredAtUtc)
            .Select(h => new ClaimTransitionDto(h.FromStatus, h.ToStatus, h.ActorId, h.Notes, h.OccurredAtUtc))
            .ToList());
}
