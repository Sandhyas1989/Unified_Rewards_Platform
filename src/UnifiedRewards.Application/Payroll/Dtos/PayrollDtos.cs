using UnifiedRewards.Domain.Enums;
using UnifiedRewards.Domain.Payroll;

namespace UnifiedRewards.Application.Payroll.Dtos;

public sealed record PayslipDto(
    Guid Id,
    Guid EmployeeId,
    int Year,
    int Month,
    decimal GrossMonthly,
    decimal TotalDeductionsMonthly,
    decimal NetMonthly,
    DateTime GeneratedAtUtc);

public sealed record SettlementRequestDto(
    Guid Id,
    Guid EmployeeId,
    decimal Amount,
    string Reference,
    SettlementStatus Status,
    int Attempts,
    string? PayrollConfirmation,
    string? LastError,
    DateTime RequestedAtUtc,
    DateTime? CompletedAtUtc);

public static class PayrollMapping
{
    public static PayslipDto ToDto(this Payslip p) => new(
        p.Id, p.EmployeeId, p.Year, p.Month, p.GrossMonthly, p.TotalDeductionsMonthly, p.NetMonthly, p.GeneratedAtUtc);

    public static SettlementRequestDto ToDto(this SettlementRequest s) => new(
        s.Id, s.EmployeeId, s.Amount, s.Reference, s.Status, s.Attempts,
        s.PayrollConfirmation, s.LastError, s.RequestedAtUtc, s.CompletedAtUtc);
}
