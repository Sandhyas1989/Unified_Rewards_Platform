using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.Common;

/// <summary>
/// Raised when a promotion nomination is asked to make a transition its state machine does not
/// allow (e.g. approving an already-rejected nomination). Mapped to HTTP 409 Conflict.
/// </summary>
public sealed class InvalidPromotionTransitionException : DomainConflictException
{
    public InvalidPromotionTransitionException(PromotionStatus from, PromotionStatus to)
        : base($"Cannot transition a promotion from '{from}' to '{to}'.")
    {
    }
}
