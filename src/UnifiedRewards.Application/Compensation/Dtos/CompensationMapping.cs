using UnifiedRewards.Domain.Compensation;

namespace UnifiedRewards.Application.Compensation.Dtos;

/// <summary>Maps the CompensationStructure aggregate (with its components) to its DTO.</summary>
public static class CompensationMapping
{
    public static CompensationStructureDto ToDto(this CompensationStructure s) => new(
        s.Id,
        s.EmployeeId,
        s.Grade,
        s.AnnualBasic,
        s.EffectiveFrom,
        s.Status,
        s.GrossAnnual,
        s.TotalDeductions,
        s.NetAnnual,
        s.Components
            .OrderBy(c => c.Type)
            .Select(c => new CompensationComponentDto(c.Name, c.Amount, c.Type))
            .ToList());
}
