using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Interfaces;

namespace UnifiedRewards.Application.Benefits.Commands.CreateBenefitPlan;

public sealed class CreateBenefitPlanCommandValidator : AbstractValidator<CreateBenefitPlanCommand>
{
    public CreateBenefitPlanCommandValidator(IApplicationDbContext db)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150)
            .MustAsync(async (name, ct) =>
                !await db.BenefitPlans.AnyAsync(p => p.Name == name.Trim(), ct))
            .WithMessage("A benefit plan with this name already exists.");

        RuleFor(x => x.Description).MaximumLength(1000);

        RuleFor(x => x.Category).IsInEnum();

        RuleFor(x => x.MonthlyCost).GreaterThanOrEqualTo(0);
    }
}
