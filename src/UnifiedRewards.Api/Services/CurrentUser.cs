using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UnifiedRewards.Application.Common.Interfaces;

namespace UnifiedRewards.Api.Services;

/// <summary><see cref="ICurrentUser"/> backed by the current HTTP request's authenticated principal.</summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                        ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(JwtRegisteredClaimNames.Email)
                            ?? Principal?.FindFirstValue(ClaimTypes.Email);
}
