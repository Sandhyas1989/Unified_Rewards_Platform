using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Claims.Dtos;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Domain.Claims;

namespace UnifiedRewards.Application.Claims.Commands.ApproveClaim;

public sealed record ApproveClaimCommand(Guid ClaimId, Guid ReviewerId, string? Notes) : IRequest<ClaimDto>;

public sealed class ApproveClaimCommandHandler : IRequestHandler<ApproveClaimCommand, ClaimDto>
{
    private readonly IApplicationDbContext _db;

    public ApproveClaimCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ClaimDto> Handle(ApproveClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _db.Claims
            .Include(c => c.History)
            .FirstOrDefaultAsync(c => c.Id == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        claim.Approve(request.ReviewerId, request.Notes);
        await _db.SaveChangesAsync(cancellationToken);

        return claim.ToDto();
    }
}
