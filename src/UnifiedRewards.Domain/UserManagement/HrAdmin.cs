using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.UserManagement;

/// <summary>
/// HR administrator. Configures benefit plans, compensation rules and
/// promotion cycles.
/// </summary>
public class HrAdmin : User
{
    public HrAdmin()
    {
        Role = UserRole.HrAdmin;
    }
}
