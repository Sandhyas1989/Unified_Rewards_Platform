using FluentValidation;

namespace UnifiedRewards.Application.Compensation.Commands.GenerateCompensation;

public sealed class GenerateCompensationCommandValidator : AbstractValidator<GenerateCompensationCommand>
{
    public GenerateCompensationCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();

        RuleFor(x => x.Grade).IsInEnum();

        RuleFor(x => x.AnnualBasic).GreaterThan(0);

        RuleFor(x => x.EffectiveFrom).NotEqual(default(DateOnly));
    }
}
