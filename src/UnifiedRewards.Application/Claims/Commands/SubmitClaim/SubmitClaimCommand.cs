using MediatR;
using UnifiedRewards.Application.Claims.Dtos;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Claims.Commands.SubmitClaim;

/// <summary>
/// Submits a reimbursement claim. <see cref="EmployeeId"/> comes from the authenticated
/// user. An optional receipt is stored via IFileStorage and OCR-scanned via IOcrEngine.
/// </summary>
public sealed record SubmitClaimCommand(
    Guid EmployeeId,
    ClaimType Type,
    decimal Amount,
    string Description,
    byte[]? Receipt,
    string? ReceiptFileName,
    string? ReceiptContentType) : IRequest<ClaimDto>;
