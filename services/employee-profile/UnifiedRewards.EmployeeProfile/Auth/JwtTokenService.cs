using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using UnifiedRewards.EmployeeProfile.Domain;

namespace UnifiedRewards.EmployeeProfile.Auth;

// Issues JWTs for this service (ported from the monolith's JwtTokenService).
// Signs with RS256 (asymmetric). Consumer services only need the public key — private key stays here.
// In production this responsibility moves to Microsoft Entra ID; Jwt:Authority in appsettings.Production.json.
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

        using var rsa = RSA.Create();
        rsa.FromXmlString(jwt["RsaPrivateKey"]!);
        var key = new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: true));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.RsaSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
