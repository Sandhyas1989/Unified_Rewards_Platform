using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Claims.Dtos;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Common.Interfaces.Integration;
using UnifiedRewards.Domain.Claims;

namespace UnifiedRewards.Application.Claims.Queries.DownloadReceipt;

/// <summary>
/// Streams a claim's receipt. A non-reviewer may only download their own claim's receipt.
/// </summary>
public sealed record DownloadReceiptQuery(Guid ClaimId, Guid RequesterId, bool IsReviewer) : IRequest<ReceiptFile>;

public sealed class DownloadReceiptQueryHandler : IRequestHandler<DownloadReceiptQuery, ReceiptFile>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _fileStorage;

    public DownloadReceiptQueryHandler(IApplicationDbContext db, IFileStorage fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    public async Task<ReceiptFile> Handle(DownloadReceiptQuery request, CancellationToken cancellationToken)
    {
        var claim = await _db.Claims
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        if (!request.IsReviewer && claim.EmployeeId != request.RequesterId)
        {
            throw new UnauthorizedAccessException("You may only download your own claim receipts.");
        }

        if (claim.ReceiptReference is null)
        {
            throw new NotFoundException("Receipt", request.ClaimId);
        }

        var content = await _fileStorage.DownloadAsync(claim.ReceiptReference, cancellationToken);
        return new ReceiptFile(content, claim.ReceiptFileName ?? "receipt", claim.ReceiptContentType ?? "application/octet-stream");
    }
}
