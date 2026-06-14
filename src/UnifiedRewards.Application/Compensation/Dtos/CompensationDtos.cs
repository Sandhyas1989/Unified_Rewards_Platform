using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Compensation.Dtos;

public sealed record CompensationComponentDto(string Name, decimal Amount, ComponentType Type);

public sealed record CompensationStructureDto(
    Guid Id,
    Guid EmployeeId,
    GradeBand Grade,
    decimal AnnualBasic,
    DateOnly EffectiveFrom,
    CompensationStatus Status,
    decimal GrossAnnual,
    decimal TotalDeductions,
    decimal NetAnnual,
    IReadOnlyList<CompensationComponentDto> Components);
