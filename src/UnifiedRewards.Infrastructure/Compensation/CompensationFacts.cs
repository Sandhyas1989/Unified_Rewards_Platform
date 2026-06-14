using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Infrastructure.Compensation;

/// <summary>Input fact inserted into the NRules session: the basic salary and grade band.</summary>
public sealed record CompensationRequest(decimal AnnualBasic, GradeBand Grade);

/// <summary>Output fact produced by a rule: one computed compensation line.</summary>
public sealed record ComponentResult(string Name, decimal Amount, ComponentType Type);
