using UnifiedRewards.Domain.Common;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.UserManagement;

/// <summary>
/// Abstract base of the User Management module. Persisted with EF Core
/// Table-Per-Hierarchy (single Users table + discriminator).
/// </summary>
public abstract class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;
}
