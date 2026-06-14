using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.UserManagement.Dtos;

public sealed record UserDto(Guid Id, string FullName, string Email, UserRole Role, bool IsActive);

public sealed record AuthResult(string Token, DateTime ExpiresAtUtc, UserDto User);
