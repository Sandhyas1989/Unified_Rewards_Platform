namespace UnifiedRewards.Domain.Enums;

/// <summary>Lifecycle states of a reimbursement claim, governed by an explicit state machine.</summary>
public enum ClaimStatus
{
    Submitted = 0,
    UnderReview = 1,
    Approved = 2,
    Rejected = 3,
    Settled = 4
}
