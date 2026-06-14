using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Benefits.Dtos;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Common.Interfaces.Integration;
using UnifiedRewards.Domain.Benefits;

namespace UnifiedRewards.Application.Benefits.Commands.EnrollInBenefit;

public sealed class EnrollInBenefitCommandHandler : IRequestHandler<EnrollInBenefitCommand, BenefitEnrollmentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _emailService;

    public EnrollInBenefitCommandHandler(IApplicationDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    public async Task<BenefitEnrollmentDto> Handle(EnrollInBenefitCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.BenefitPlans
            .FirstOrDefaultAsync(p => p.Id == request.BenefitPlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(BenefitPlan), request.BenefitPlanId);

        var employee = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("User", request.EmployeeId);

        var enrollment = new BenefitEnrollment
        {
            EmployeeId = request.EmployeeId,
            BenefitPlanId = plan.Id,
            CoverageStartDate = request.CoverageStartDate,
            Status = Domain.Enums.EnrollmentStatus.Active
        };

        _db.BenefitEnrollments.Add(enrollment);
        await _db.SaveChangesAsync(cancellationToken);

        // Integration seam: confirmation email (logged locally, Azure Communication Services in cloud).
        await _emailService.SendAsync(
            employee.Email,
            $"Enrolled in {plan.Name}",
            $"You are enrolled in '{plan.Name}' with coverage starting {enrollment.CoverageStartDate:yyyy-MM-dd}.",
            cancellationToken);

        return new BenefitEnrollmentDto(
            enrollment.Id,
            enrollment.EmployeeId,
            enrollment.BenefitPlanId,
            plan.Name,
            enrollment.CoverageStartDate,
            enrollment.Status);
    }
}
