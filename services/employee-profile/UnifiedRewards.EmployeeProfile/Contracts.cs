using UnifiedRewards.EmployeeProfile.Domain;

namespace UnifiedRewards.EmployeeProfile;

public sealed record UserDto(Guid Id, string FullName, string Email, UserRole Role, bool IsActive, string? Grade);
public sealed record AuthResult(string Token, DateTime ExpiresAtUtc, UserDto User);
public sealed record LoginRequest(string Email, string Password);
public sealed record CreateEmployeeRequest(string FullName, string Email, string Password, string Grade, DateOnly DateOfJoining, Guid? ManagerId);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
