using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Claims.Dtos;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Domain.Claims;

namespace UnifiedRewards.Application.Claims.Commands.RejectClaim;

public sealed record RejectClaimCommand(Guid ClaimId, Guid ReviewerId, string? Notes) : IRequest<ClaimDto>;

public sealed class RejectClaimCommandHandler : IRequestHandler<RejectClaimCommand, ClaimDto>
{
    private readonly IApplicationDbContext _db;

    public RejectClaimCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ClaimDto> Handle(RejectClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _db.Claims
            .Include(c => c.History)
            .FirstOrDefaultAsync(c => c.Id == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        claim.Reject(request.ReviewerId, request.Notes);
        await _db.SaveChangesAsync(cancellationToken);

        return claim.ToDto();
    }
}
