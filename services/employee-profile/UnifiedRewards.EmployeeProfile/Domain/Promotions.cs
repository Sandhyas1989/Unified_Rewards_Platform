namespace UnifiedRewards.EmployeeProfile.Domain;

public enum PromotionStatus { Draft = 0, Open = 1, Closed = 2, Cancelled = 3 }
public enum NominationOutcome { Pending = 0, Promoted = 1, NotPromoted = 2, Withdrawn = 3 }

/// <summary>
/// A time-bounded bonus campaign created by HR Admin (e.g., "Year-End Bonus Q4 2025", "Diwali
/// Voucher Q3 2025"). Defines the bonus payout value, optional eligible grade filter, nomination
/// window, and optional eligibility criteria. Lifecycle: Draft → Open → Closed | Cancelled.
/// </summary>
public class Promotion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CreatedBy { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CycleYear { get; set; }
    public string CycleQuarter { get; set; } = string.Empty;   // Q1|Q2|Q3|Q4
    public string FromGrade { get; set; } = string.Empty;      // optional: grade employees must hold to be eligible (empty = all grades eligible)
    public decimal BonusValue { get; set; }                    // monetary payout per approved nominee
    public DateOnly NominationStart { get; set; }
    public DateOnly NominationEnd { get; set; }
    public PromotionStatus Status { get; private set; } = PromotionStatus.Draft;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public PromotionEligibility? Eligibility { get; set; }
    public List<PromotionNomination> Nominations { get; set; } = new();

    public void Open()
    {
        if (Status != PromotionStatus.Draft) throw new InvalidOperationException("Only a Draft cycle can be opened.");
        Status = PromotionStatus.Open; UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Close()
    {
        if (Status != PromotionStatus.Open) throw new InvalidOperationException("Only an Open cycle can be closed.");
        Status = PromotionStatus.Closed; UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is PromotionStatus.Closed or PromotionStatus.Cancelled)
            throw new InvalidOperationException("Cannot cancel a Closed or already Cancelled cycle.");
        Status = PromotionStatus.Cancelled; UpdatedAtUtc = DateTime.UtcNow;
    }
}

/// <summary>
/// Optional eligibility criteria owned by a Promotion cycle (cascade-deleted with it).
/// At nomination time these are evaluated and the result is recorded in PromotionEligibilityCheck.
/// </summary>
public class PromotionEligibility
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PromotionId { get; set; }
    public int MinTenureMonths { get; set; }
    public string? MinPerformanceRating { get; set; }   // e.g. "3.5"
    public string? MinCurrentGrade { get; set; }
    public string? LocationCodes { get; set; }          // JSON array of eligible location codes
}

/// <summary>
/// A manager nominating an employee into a Promotion cycle. UNIQUE on (PromotionId, EmployeeId).
/// Outcome starts Pending; HR Admin drives it to Promoted | NotPromoted. On Promoted the
/// PromotionsController updates the employee's grade in the Users table.
/// </summary>
public class PromotionNomination
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PromotionId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid NominatedBy { get; set; }
    public DateOnly NominatedOn { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public NominationOutcome Outcome { get; private set; } = NominationOutcome.Pending;
    public string? Remarks { get; set; }
    public Guid? OutcomeUpdatedBy { get; set; }
    public DateTime? OutcomeUpdatedOn { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public void Approve(Guid actorId)
    {
        if (Outcome != NominationOutcome.Pending) throw new InvalidOperationException("Nomination is not Pending.");
        Outcome = NominationOutcome.Promoted; OutcomeUpdatedBy = actorId; OutcomeUpdatedOn = DateTime.UtcNow;
    }

    public void Reject(Guid actorId, string? remarks = null)
    {
        if (Outcome != NominationOutcome.Pending) throw new InvalidOperationException("Nomination is not Pending.");
        Outcome = NominationOutcome.NotPromoted; OutcomeUpdatedBy = actorId; OutcomeUpdatedOn = DateTime.UtcNow;
        if (remarks is not null) Remarks = remarks;
    }

    public void Withdraw(Guid actorId)
    {
        if (Outcome != NominationOutcome.Pending) throw new InvalidOperationException("Nomination is not Pending.");
        Outcome = NominationOutcome.Withdrawn; OutcomeUpdatedBy = actorId; OutcomeUpdatedOn = DateTime.UtcNow;
    }
}

/// <summary>
/// Immutable point-in-time snapshot of an eligibility evaluation run at nomination time.
/// Supports audit ("why was someone eligible / not eligible at the time of nomination?").
/// </summary>
public class PromotionEligibilityCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PromotionId { get; set; }
    public Guid EmployeeId { get; set; }
    public bool IsEligible { get; set; }
    public DateTime CheckedOn { get; set; } = DateTime.UtcNow;
    public int TenureMonths { get; set; }
    public string? PerformanceRating { get; set; }
    public string? FailureReason { get; set; }
}
