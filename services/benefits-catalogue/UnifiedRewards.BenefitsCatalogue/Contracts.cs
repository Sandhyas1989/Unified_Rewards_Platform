namespace UnifiedRewards.BenefitsCatalogue;

public sealed record BenefitPlanDto(Guid Id, string Name, string Description, int Category, decimal MonthlyCost, bool IsActive);
public sealed record CreatePlanRequest(string Name, string Description, int Category, decimal MonthlyCost);
public sealed record BenefitEnrollmentDto(Guid Id, Guid EmployeeId, Guid BenefitPlanId, string BenefitPlanName, DateOnly CoverageStartDate, int Status);
public sealed record EnrollRequest(Guid BenefitPlanId, DateOnly CoverageStartDate);
