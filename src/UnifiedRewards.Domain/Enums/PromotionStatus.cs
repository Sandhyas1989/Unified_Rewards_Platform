namespace UnifiedRewards.Domain.Enums;

/// <summary>Lifecycle of a promotion nomination, governed by a state machine.</summary>
public enum PromotionStatus
{
    Nominated = 0,
    UnderReview = 1,
    Approved = 2,
    Rejected = 3
}
