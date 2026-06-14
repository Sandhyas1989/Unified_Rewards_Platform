using UnifiedRewards.Domain.Common;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.Claims;

/// <summary>
/// An immutable audit record of a single state-machine transition on a claim.
/// Child of <see cref="Claim"/>.
/// </summary>
public class ClaimTransition : BaseEntity
{
    public Guid ClaimId { get; set; }

    /// <summary>Source state; null for the initial submission.</summary>
    public ClaimStatus? FromStatus { get; set; }

    public ClaimStatus ToStatus { get; set; }

    /// <summary>User who caused the transition (employee on submit, reviewer/finance otherwise).</summary>
    public Guid ActorId { get; set; }

    public string? Notes { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
