using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UnifiedRewards.EmployeeProfile.Domain;

namespace UnifiedRewards.EmployeeProfile.Auth;

// Issues JWTs for this service (ported from the monolith's JwtTokenService).
// In production this responsibility moves to Microsoft Entra ID; kept here for the local demo.
public sealed class JwtTokenService
{
    public const string TenantClaim = "tenant_id";
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    public (string Token, DateTime ExpiresAtUtc) CreateToken(User user)
    {
        var jwt = _config.GetSection("Jwt");
        var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiryMinutes"] ?? "120"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(TenantClaim, user.TenantId.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
