namespace UnifiedRewards.BenefitsCatalogue.Domain;

public enum BenefitCategory { Health = 0, Wellness = 1, Insurance = 2, Transport = 3, Food = 4, Education = 5 }
public enum EnrollmentStatus { Active = 0, Cancelled = 1 }

public class BenefitPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BenefitCategory Category { get; set; }
    public decimal MonthlyCost { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public bool IsActive { get; set; } = true;
}

public class BenefitEnrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid BenefitPlanId { get; set; }
    public string BenefitPlanName { get; set; } = string.Empty;
    public DateOnly CoverageStartDate { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public DateTime? CancelledAtUtc { get; set; }
}
