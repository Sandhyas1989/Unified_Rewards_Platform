using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.Common;

/// <summary>
/// Raised when a claim is asked to make a transition its state machine does not allow
/// (e.g. settling a claim that was never approved). Mapped to HTTP 409 Conflict.
/// </summary>
public sealed class InvalidClaimTransitionException : DomainConflictException
{
    public InvalidClaimTransitionException(ClaimStatus from, ClaimStatus to)
        : base($"Cannot transition a claim from '{from}' to '{to}'.")
    {
    }
}
