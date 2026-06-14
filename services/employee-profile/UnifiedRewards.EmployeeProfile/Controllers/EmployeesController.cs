using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.EmployeeProfile.Auth;
using UnifiedRewards.EmployeeProfile.Domain;
using UnifiedRewards.EmployeeProfile.Persistence;

namespace UnifiedRewards.EmployeeProfile.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public sealed class EmployeesController : ControllerBase
{
    private readonly EmployeeProfileDbContext _db;

    public EmployeesController(EmployeeProfileDbContext db) => _db = db;

    // Tenant comes from the validated JWT — every operation is scoped to the caller's tenant.
    private Guid TenantId =>
        Guid.TryParse(User.FindFirst(JwtTokenService.TenantClaim)?.Value, out var t) ? t : Guid.Empty;

    private static UserDto ToDto(User u) =>
        new(u.Id, u.FullName, u.Email, u.Role, u.IsActive, u is Employee e ? e.Grade : null);

    /// <summary>Creates an employee in the caller's tenant. HR Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<UserDto>> Create(CreateEmployeeRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.TenantId == TenantId && u.Email == email, ct))
        {
            return Conflict(new { title = "A user with this email already exists." });
        }

        var employee = new Employee
        {
            TenantId = TenantId,
            FullName = req.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(req.Password),
            Grade = req.Grade,
            DateOfJoining = req.DateOfJoining,
            ManagerId = req.ManagerId,
        };
        _db.Users.Add(employee);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, ToDto(employee));
    }

    /// <summary>Lists users in the caller's tenant (paged), optionally filtered by role.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> Get(
        [FromQuery] UserRole? role, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        var query = _db.Users.AsNoTracking().Where(u => u.TenantId == TenantId);
        if (role is not null) query = query.Where(u => u.Role == role);

        var p = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 25, 1, 200);
        var total = await query.CountAsync(ct);
        var users = await query.OrderBy(u => u.FullName).Skip((p - 1) * size).Take(size).ToListAsync(ct);
        return Ok(new PagedResult<UserDto>(users.Select(ToDto).ToList(), p, size, total));
    }

    /// <summary>Gets a single user by id (tenant-scoped).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && u.TenantId == TenantId, ct);
        return user is null ? NotFound() : Ok(ToDto(user));
    }
}
