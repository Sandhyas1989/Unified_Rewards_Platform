using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.EmployeeProfile.Auth;
using UnifiedRewards.EmployeeProfile.Persistence;

namespace UnifiedRewards.EmployeeProfile.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly EmployeeProfileDbContext _db;
    private readonly JwtTokenService _jwt;

    public AuthController(EmployeeProfileDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> Login(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { title = "Invalid email or password." });
        }

        var (token, expires) = _jwt.CreateToken(user);
        var grade = user is Domain.Employee e ? e.Grade : null;
        return Ok(new AuthResult(token, expires, new UserDto(user.Id, user.FullName, user.Email, user.Role, user.IsActive, grade)));
    }
}
