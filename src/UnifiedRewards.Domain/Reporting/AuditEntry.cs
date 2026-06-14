using UnifiedRewards.Domain.Common;

namespace UnifiedRewards.Domain.Reporting;

/// <summary>
/// An audit-trail record of a command executed against the platform. Written automatically by
/// the audit pipeline behavior, capturing the acting user, the action and the outcome.
/// </summary>
public class AuditEntry : BaseEntity
{
    public Guid? UserId { get; set; }

    public string? UserEmail { get; set; }

    /// <summary>The command type name (e.g. "ApprovePromotionCommand").</summary>
    public string Action { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? Error { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
