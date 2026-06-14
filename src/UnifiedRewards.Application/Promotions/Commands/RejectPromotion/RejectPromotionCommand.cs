using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Promotions.Dtos;
using UnifiedRewards.Domain.Promotions;

namespace UnifiedRewards.Application.Promotions.Commands.RejectPromotion;

public sealed record RejectPromotionCommand(Guid NominationId, Guid ReviewerId, string? Notes) : IRequest<PromotionNominationDto>;

public sealed class RejectPromotionCommandHandler : IRequestHandler<RejectPromotionCommand, PromotionNominationDto>
{
    private readonly IApplicationDbContext _db;

    public RejectPromotionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<PromotionNominationDto> Handle(RejectPromotionCommand request, CancellationToken cancellationToken)
    {
        var nomination = await _db.PromotionNominations
            .FirstOrDefaultAsync(n => n.Id == request.NominationId, cancellationToken)
            ?? throw new NotFoundException(nameof(PromotionNomination), request.NominationId);

        nomination.Reject(request.ReviewerId, request.Notes);
        await _db.SaveChangesAsync(cancellationToken);

        return nomination.ToDto();
    }
}
