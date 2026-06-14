using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Claims.Dtos;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Domain.Claims;

namespace UnifiedRewards.Application.Claims.Commands.StartClaimReview;

public sealed record StartClaimReviewCommand(Guid ClaimId, Guid ReviewerId) : IRequest<ClaimDto>;

public sealed class StartClaimReviewCommandHandler : IRequestHandler<StartClaimReviewCommand, ClaimDto>
{
    private readonly IApplicationDbContext _db;

    public StartClaimReviewCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ClaimDto> Handle(StartClaimReviewCommand request, CancellationToken cancellationToken)
    {
        var claim = await _db.Claims
            .Include(c => c.History)
            .FirstOrDefaultAsync(c => c.Id == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        claim.StartReview(request.ReviewerId);
        await _db.SaveChangesAsync(cancellationToken);

        return claim.ToDto();
    }
}
