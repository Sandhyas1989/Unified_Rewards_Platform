using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Promotions.Dtos;
using UnifiedRewards.Domain.Promotions;

namespace UnifiedRewards.Application.Promotions.Commands.StartPromotionReview;

public sealed record StartPromotionReviewCommand(Guid NominationId, Guid ReviewerId) : IRequest<PromotionNominationDto>;

public sealed class StartPromotionReviewCommandHandler
    : IRequestHandler<StartPromotionReviewCommand, PromotionNominationDto>
{
    private readonly IApplicationDbContext _db;

    public StartPromotionReviewCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<PromotionNominationDto> Handle(StartPromotionReviewCommand request, CancellationToken cancellationToken)
    {
        var nomination = await _db.PromotionNominations
            .FirstOrDefaultAsync(n => n.Id == request.NominationId, cancellationToken)
            ?? throw new NotFoundException(nameof(PromotionNomination), request.NominationId);

        nomination.StartReview(request.ReviewerId);
        await _db.SaveChangesAsync(cancellationToken);

        return nomination.ToDto();
    }
}
