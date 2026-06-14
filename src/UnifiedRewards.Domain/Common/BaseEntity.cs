namespace UnifiedRewards.Domain.Common;

/// <summary>
/// Base type for all persisted aggregate roots / entities.
/// Provides a surrogate key and audit timestamps (UTC).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
