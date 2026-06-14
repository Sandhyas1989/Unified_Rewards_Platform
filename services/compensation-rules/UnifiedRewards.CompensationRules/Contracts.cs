namespace UnifiedRewards.CompensationRules;

public sealed record CompensationComponentDto(string Name, decimal Amount, int Type);
public sealed record CompensationStructureDto(
    Guid Id, Guid EmployeeId, int Grade, decimal AnnualBasic, DateOnly EffectiveFrom, int Status,
    decimal GrossAnnual, decimal TotalDeductions, decimal NetAnnual, IReadOnlyList<CompensationComponentDto> Components);
public sealed record GenerateCompensationRequest(Guid EmployeeId, int Grade, decimal AnnualBasic, DateOnly EffectiveFrom);
