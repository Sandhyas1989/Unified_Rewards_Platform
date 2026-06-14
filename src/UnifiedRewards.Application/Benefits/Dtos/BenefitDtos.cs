using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Benefits.Dtos;

public sealed record BenefitPlanDto(
    Guid Id,
    string Name,
    string Description,
    BenefitCategory Category,
    decimal MonthlyCost,
    bool IsActive);

public sealed record BenefitEnrollmentDto(
    Guid Id,
    Guid EmployeeId,
    Guid BenefitPlanId,
    string BenefitPlanName,
    DateOnly CoverageStartDate,
    EnrollmentStatus Status);
