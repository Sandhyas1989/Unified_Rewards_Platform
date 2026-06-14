using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Common.Interfaces;

/// <summary>A single computed compensation line.</summary>
public sealed record CompensationLine(string Name, decimal Amount, ComponentType Type);

/// <summary>The full rule-derived breakdown for an annual basic + grade band.</summary>
public sealed record CompensationBreakdown(
    IReadOnlyList<CompensationLine> Lines,
    decimal GrossAnnual,
    decimal TotalDeductions,
    decimal NetAnnual);

/// <summary>
/// Computes a compensation breakdown from an annual basic and grade band by
/// running the business rules. Implemented in Infrastructure with NRules so the
/// Application layer stays free of the rules-engine dependency.
/// </summary>
public interface ICompensationCalculator
{
    CompensationBreakdown Calculate(decimal annualBasic, GradeBand grade);
}
