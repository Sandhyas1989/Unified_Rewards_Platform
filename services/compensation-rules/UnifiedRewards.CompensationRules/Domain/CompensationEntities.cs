namespace UnifiedRewards.CompensationRules.Domain;

public enum GradeBand { Junior = 0, Mid = 1, Senior = 2, Lead = 3 }
public enum ComponentType { Earning = 0, Deduction = 1 }
public enum CompensationStatus { Draft = 0, Approved = 1 }

public class CompensationStructure
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; set; }
    public GradeBand Grade { get; set; }
    public decimal AnnualBasic { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public CompensationStatus Status { get; private set; } = CompensationStatus.Draft;
    public decimal GrossAnnual { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetAnnual { get; set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public ICollection<CompensationComponent> Components { get; set; } = new List<CompensationComponent>();

    public void Approve() { Status = CompensationStatus.Approved; ApprovedAtUtc = DateTime.UtcNow; }
}

public class CompensationComponent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompensationStructureId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ComponentType Type { get; set; }
}
