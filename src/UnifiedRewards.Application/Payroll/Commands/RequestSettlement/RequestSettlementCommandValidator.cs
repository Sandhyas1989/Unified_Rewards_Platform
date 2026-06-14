using FluentValidation;

namespace UnifiedRewards.Application.Payroll.Commands.RequestSettlement;

public sealed class RequestSettlementCommandValidator : AbstractValidator<RequestSettlementCommand>
{
    public RequestSettlementCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
