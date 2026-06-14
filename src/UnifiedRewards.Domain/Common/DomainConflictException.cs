namespace UnifiedRewards.Domain.Common;

/// <summary>
/// Base for domain-rule violations that represent a conflict with the current state of an
/// aggregate (e.g. an illegal state-machine transition). Mapped to HTTP 409 Conflict.
/// </summary>
public abstract class DomainConflictException : Exception
{
    protected DomainConflictException(string message) : base(message)
    {
    }
}
