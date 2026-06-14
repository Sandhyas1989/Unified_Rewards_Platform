using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.UserManagement;

/// <summary>
/// A rank-and-file employee. Self-service consumer of benefits, claims, CTC.
/// </summary>
public class Employee : User
{
    public string Grade { get; set; } = string.Empty;

    public DateOnly DateOfJoining { get; set; }

    /// <summary>Reporting manager (self-reference to another User).</summary>
    public Guid? ManagerId { get; set; }

    public Employee()
    {
        Role = UserRole.Employee;
    }
}
