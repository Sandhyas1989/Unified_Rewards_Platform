using UnifiedRewards.Domain.Common;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.Benefits;

/// <summary>
/// An employee's enrolment in a <see cref="BenefitPlan"/>. Links a User (employee)
/// to a plan and tracks the coverage start and lifecycle status.
/// </summary>
public class BenefitEnrollment : BaseEntity
{
    /// <summary>The enrolling user (FK to Users.Id).</summary>
    public Guid EmployeeId { get; set; }

    public Guid BenefitPlanId { get; set; }

    public BenefitPlan? BenefitPlan { get; set; }

    public DateOnly CoverageStartDate { get; set; }

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

    public DateTime? CancelledAtUtc { get; set; }
}
