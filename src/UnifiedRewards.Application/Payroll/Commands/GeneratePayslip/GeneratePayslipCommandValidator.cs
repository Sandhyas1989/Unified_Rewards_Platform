using FluentValidation;

namespace UnifiedRewards.Application.Payroll.Commands.GeneratePayslip;

public sealed class GeneratePayslipCommandValidator : AbstractValidator<GeneratePayslipCommand>
{
    public GeneratePayslipCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
