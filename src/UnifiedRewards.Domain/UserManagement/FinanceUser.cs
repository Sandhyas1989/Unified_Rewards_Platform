using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.UserManagement;

/// <summary>
/// Finance / Audit user. Generates reports and reviews the audit trail.
/// </summary>
public class FinanceUser : User
{
    public FinanceUser()
    {
        Role = UserRole.Finance;
    }
}
