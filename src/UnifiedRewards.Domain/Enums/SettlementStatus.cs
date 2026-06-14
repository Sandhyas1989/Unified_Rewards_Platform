namespace UnifiedRewards.Domain.Enums;

/// <summary>Lifecycle of an asynchronous payroll settlement request.</summary>
public enum SettlementStatus
{
    Pending = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3
}
