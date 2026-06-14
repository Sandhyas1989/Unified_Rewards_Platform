using UnifiedRewards.PayrollIntegration.Domain;

namespace UnifiedRewards.PayrollIntegration;

public sealed record SettlementDto(Guid Id, Guid EmployeeId, decimal Amount, string Reference,
    SettlementStatus Status, int Attempts, string? PayrollConfirmation, string? LastError,
    DateTime RequestedAtUtc, DateTime? CompletedAtUtc);

public sealed record RequestSettlementRequest(Guid EmployeeId, decimal Amount);

public sealed record PayslipDto(Guid Id, Guid EmployeeId, int Year, int Month,
    decimal GrossMonthly, decimal TotalDeductionsMonthly, decimal NetMonthly, DateTime GeneratedAtUtc);

public sealed record GeneratePayslipRequest(Guid EmployeeId, int Year, int Month,
    decimal GrossMonthly, decimal TotalDeductionsMonthly, decimal NetMonthly);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
