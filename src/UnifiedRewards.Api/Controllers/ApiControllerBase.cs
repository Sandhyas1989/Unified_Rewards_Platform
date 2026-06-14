using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace UnifiedRewards.Api.Controllers;

/// <summary>Base for API controllers; exposes the authenticated user's id from the JWT.</summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>The authenticated user's id, read from the JWT "sub" / NameIdentifier claim.</summary>
    protected Guid CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id)
                ? id
                : throw new UnauthorizedAccessException("Token does not contain a valid user id.");
        }
    }
}
