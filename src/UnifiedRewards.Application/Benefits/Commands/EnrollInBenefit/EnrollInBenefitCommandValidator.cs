using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Benefits.Commands.EnrollInBenefit;

public sealed class EnrollInBenefitCommandValidator : AbstractValidator<EnrollInBenefitCommand>
{
    public EnrollInBenefitCommandValidator(IApplicationDbContext db)
    {
        RuleFor(x => x.EmployeeId).NotEmpty();

        RuleFor(x => x.CoverageStartDate).NotEqual(default(DateOnly));

        RuleFor(x => x.BenefitPlanId)
            .NotEmpty()
            .MustAsync(async (planId, ct) =>
                await db.BenefitPlans.AnyAsync(p => p.Id == planId && p.IsActive, ct))
            .WithMessage("The benefit plan does not exist or is not open for enrolment.");

        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
                !await db.BenefitEnrollments.AnyAsync(e =>
                    e.EmployeeId == cmd.EmployeeId &&
                    e.BenefitPlanId == cmd.BenefitPlanId &&
                    e.Status == EnrollmentStatus.Active, ct))
            .WithMessage("Employee is already actively enrolled in this benefit plan.");
    }
}
