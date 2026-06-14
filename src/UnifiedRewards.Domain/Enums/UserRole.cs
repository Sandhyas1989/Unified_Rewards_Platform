namespace UnifiedRewards.Domain.Enums;

/// <summary>
/// The four platform roles. Used for [Authorize(Roles = ...)] and JWT role claims.
/// </summary>
public enum UserRole
{
    Employee = 0,
    Manager = 1,
    HrAdmin = 2,
    Finance = 3
}
