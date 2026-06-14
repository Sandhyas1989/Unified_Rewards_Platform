using UnifiedRewards.Domain.Common;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.Compensation;

/// <summary>
/// An employee's annual compensation package. The component breakdown and the
/// gross/net totals are derived by the NRules-based calculator from the annual
/// basic and grade band. Aggregate root of the Compensation module.
/// </summary>
public class CompensationStructure : BaseEntity
{
    /// <summary>The employee this package belongs to (FK-less id → Users.Id).</summary>
    public Guid EmployeeId { get; set; }

    public GradeBand Grade { get; set; }

    /// <summary>Annual basic salary that the rules expand into the full breakdown.</summary>
    public decimal AnnualBasic { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public CompensationStatus Status { get; set; } = CompensationStatus.Draft;

    public decimal GrossAnnual { get; set; }

    public decimal TotalDeductions { get; set; }

    public decimal NetAnnual { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public ICollection<CompensationComponent> Components { get; set; } = new List<CompensationComponent>();
}
