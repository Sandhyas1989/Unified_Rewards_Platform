using MediatR;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Promotions.Dtos;
using UnifiedRewards.Domain.Promotions;

namespace UnifiedRewards.Application.Promotions.Commands.NominateForPromotion;

public sealed class NominateForPromotionCommandHandler
    : IRequestHandler<NominateForPromotionCommand, PromotionNominationDto>
{
    private readonly IApplicationDbContext _db;

    public NominateForPromotionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<PromotionNominationDto> Handle(NominateForPromotionCommand request, CancellationToken cancellationToken)
    {
        var nomination = PromotionNomination.Nominate(
            request.EmployeeId, request.NominatedById, request.CurrentGrade, request.ProposedGrade, request.Justification);

        _db.PromotionNominations.Add(nomination);
        await _db.SaveChangesAsync(cancellationToken);

        return nomination.ToDto();
    }
}
