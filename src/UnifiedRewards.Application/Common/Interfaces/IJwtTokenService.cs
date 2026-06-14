using UnifiedRewards.Domain.UserManagement;

namespace UnifiedRewards.Application.Common.Interfaces;

/// <summary>Issues signed JWT bearer tokens carrying the user's id, email and role.</summary>
public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
}
