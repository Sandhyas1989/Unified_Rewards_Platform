using UnifiedRewards.Domain.Common;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.Benefits;

/// <summary>
/// A benefit offering defined by HR (e.g. Health Insurance, Gym Membership, Meal Card)
/// that employees can enrol in. Aggregate root of the Benefits module.
/// </summary>
public class BenefitPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public BenefitCategory Category { get; set; }

    /// <summary>Indicative monthly cost/value of the benefit, in platform currency.</summary>
    public decimal MonthlyCost { get; set; }

    /// <summary>When false, the plan is closed to new enrolments.</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<BenefitEnrollment> Enrollments { get; set; } = new List<BenefitEnrollment>();
}
