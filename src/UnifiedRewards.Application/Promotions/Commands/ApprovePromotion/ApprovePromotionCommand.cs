using MediatR;
using UnifiedRewards.Application.Promotions.Dtos;

namespace UnifiedRewards.Application.Promotions.Commands.ApprovePromotion;

public sealed record ApprovePromotionCommand(
    Guid NominationId,
    Guid ReviewerId,
    DateOnly EffectiveDate,
    string? Notes) : IRequest<PromotionNominationDto>;
