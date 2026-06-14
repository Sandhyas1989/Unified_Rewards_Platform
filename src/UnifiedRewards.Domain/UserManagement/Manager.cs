using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.UserManagement;

/// <summary>
/// A people manager. Inherits employee attributes and additionally approves
/// claims and raises promotion nominations for their team.
/// </summary>
public class Manager : Employee
{
    public Manager()
    {
        Role = UserRole.Manager;
    }
}
