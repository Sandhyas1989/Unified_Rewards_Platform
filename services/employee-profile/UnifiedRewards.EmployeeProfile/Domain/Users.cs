namespace UnifiedRewards.EmployeeProfile.Domain;

// Ported from the monolith's User Management module (TPH hierarchy), now owned by this service.
public enum UserRole { Employee = 0, Manager = 1, HrAdmin = 2, Finance = 3 }

public abstract class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }              // multi-tenant: every row is tenant-scoped
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Employee : User
{
    public string Grade { get; set; } = string.Empty;
    public DateOnly DateOfJoining { get; set; }
    public Guid? ManagerId { get; set; }
    public Employee() { Role = UserRole.Employee; }
}

public class Manager : Employee { public Manager() { Role = UserRole.Manager; } }
public class HrAdmin : User { public HrAdmin() { Role = UserRole.HrAdmin; } }
public class FinanceUser : User { public FinanceUser() { Role = UserRole.Finance; } }
