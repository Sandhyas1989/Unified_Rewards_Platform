using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Domain.Benefits;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Benefits.Commands.CancelEnrollment;

public sealed class CancelEnrollmentCommandHandler : IRequestHandler<CancelEnrollmentCommand>
{
    private readonly IApplicationDbContext _db;

    public CancelEnrollmentCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(CancelEnrollmentCommand request, CancellationToken cancellationToken)
    {
        // Scope the lookup to the caller so one employee cannot cancel another's enrolment;
        // a mismatch is indistinguishable from "not found".
        var enrollment = await _db.BenefitEnrollments
            .FirstOrDefaultAsync(
                e => e.Id == request.EnrollmentId && e.EmployeeId == request.EmployeeId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(BenefitEnrollment), request.EnrollmentId);

        if (enrollment.Status == EnrollmentStatus.Cancelled)
        {
            return;
        }

        enrollment.Status = EnrollmentStatus.Cancelled;
        enrollment.CancelledAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
