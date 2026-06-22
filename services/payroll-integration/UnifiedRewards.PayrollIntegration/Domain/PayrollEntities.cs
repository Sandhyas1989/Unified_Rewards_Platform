using System.ComponentModel.DataAnnotations;

namespace UnifiedRewards.PayrollIntegration.Domain;

public enum SettlementStatus { Pending = 0, Processing = 1, Succeeded = 2, Failed = 3 }

// An asynchronous payroll settlement (ported from the monolith's Payroll module).
public class SettlementRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ClaimId { get; set; }   // correlation: the reimbursement claim this settlement covers
    public Guid EmployeeId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public string Reference { get; set; } = string.Empty;
    public SettlementStatus Status { get; private set; } = SettlementStatus.Pending;
    public int Attempts { get; private set; }
    public string? PayrollConfirmation { get; private set; }
    public string? LastError { get; private set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; private set; }

    // Optimistic-concurrency token — store-generated rowversion on SQL Server only (see PayrollDbContext);
    // plain column on SQLite/local so inserts don't hit a NOT NULL/store-generation mismatch.
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public void MarkProcessing() { Status = SettlementStatus.Processing; Attempts++; }
    public void MarkSucceeded(string confirmation) { Status = SettlementStatus.Succeeded; PayrollConfirmation = confirmation; CompletedAtUtc = DateTime.UtcNow; }
    public void MarkFailed(string error) { Status = SettlementStatus.Failed; LastError = error; CompletedAtUtc = DateTime.UtcNow; }
}

// Monthly payslip owned by this service.
public class Payslip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal GrossMonthly { get; set; }
    public decimal TotalDeductionsMonthly { get; set; }
    public decimal NetMonthly { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
