using UnifiedRewards.Domain.Common;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.Payroll;

/// <summary>
/// An asynchronous payout to the external payroll system. Created in <see cref="SettlementStatus.Pending"/>,
/// then advanced by the background settlement worker (which pushes through a Polly resilience pipeline).
/// </summary>
public class SettlementRequest : BaseEntity
{
    public Guid EmployeeId { get; set; }

    public decimal Amount { get; set; }

    public string Reference { get; set; } = string.Empty;

    public SettlementStatus Status { get; private set; } = SettlementStatus.Pending;

    /// <summary>Number of worker processing attempts (resilient retries happen within an attempt).</summary>
    public int Attempts { get; private set; }

    public string? PayrollConfirmation { get; private set; }

    public string? LastError { get; private set; }

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; private set; }

    public void MarkProcessing()
    {
        Status = SettlementStatus.Processing;
        Attempts++;
    }

    public void MarkSucceeded(string confirmation)
    {
        Status = SettlementStatus.Succeeded;
        PayrollConfirmation = confirmation;
        LastError = null;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = SettlementStatus.Failed;
        LastError = error;
        CompletedAtUtc = DateTime.UtcNow;
    }
}
