using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Common.Interfaces.Integration;
using UnifiedRewards.Application.Promotions.Dtos;
using UnifiedRewards.Application.Promotions.Events;
using UnifiedRewards.Domain.Promotions;

namespace UnifiedRewards.Application.Promotions.Commands.ApprovePromotion;

public sealed class ApprovePromotionCommandHandler : IRequestHandler<ApprovePromotionCommand, PromotionNominationDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventBus _eventBus;

    public ApprovePromotionCommandHandler(IApplicationDbContext db, IEventBus eventBus)
    {
        _db = db;
        _eventBus = eventBus;
    }

    public async Task<PromotionNominationDto> Handle(ApprovePromotionCommand request, CancellationToken cancellationToken)
    {
        var nomination = await _db.PromotionNominations
            .FirstOrDefaultAsync(n => n.Id == request.NominationId, cancellationToken)
            ?? throw new NotFoundException(nameof(PromotionNomination), request.NominationId);

        nomination.Approve(request.ReviewerId, request.Notes, request.EffectiveDate);
        await _db.SaveChangesAsync(cancellationToken);

        // Decoupled reaction (email the employee, etc.) via the event bus.
        await _eventBus.PublishAsync(
            new PromotionApprovedEvent(nomination.Id, nomination.EmployeeId, nomination.ProposedGrade, request.EffectiveDate),
            cancellationToken);

        return nomination.ToDto();
    }
}
