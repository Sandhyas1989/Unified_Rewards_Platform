using FluentValidation;

namespace UnifiedRewards.Application.Promotions.Commands.ApprovePromotion;

public sealed class ApprovePromotionCommandValidator : AbstractValidator<ApprovePromotionCommand>
{
    public ApprovePromotionCommandValidator()
    {
        RuleFor(x => x.NominationId).NotEmpty();
        RuleFor(x => x.EffectiveDate).NotEqual(default(DateOnly));
    }
}
