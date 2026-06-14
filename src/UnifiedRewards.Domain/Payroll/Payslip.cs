using UnifiedRewards.Domain.Common;

namespace UnifiedRewards.Domain.Payroll;

/// <summary>
/// A monthly payslip for an employee, derived from their latest approved compensation
/// structure (annual figures divided across 12 months). Aggregate root.
/// </summary>
public class Payslip : BaseEntity
{
    public Guid EmployeeId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal GrossMonthly { get; set; }

    public decimal TotalDeductionsMonthly { get; set; }

    public decimal NetMonthly { get; set; }

    /// <summary>The compensation structure this payslip was generated from.</summary>
    public Guid CompensationStructureId { get; set; }

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
