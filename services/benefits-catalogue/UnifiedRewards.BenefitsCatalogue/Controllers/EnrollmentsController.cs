using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.BenefitsCatalogue.Domain;
using UnifiedRewards.BenefitsCatalogue.Persistence;

namespace UnifiedRewards.BenefitsCatalogue.Controllers;

[ApiController]
[Route("api/enrollments")]
[Authorize]
public sealed class EnrollmentsController : ControllerBase
{
    public const string TenantClaim = "tenant_id";
    private readonly BenefitsDbContext _db;
    public EnrollmentsController(BenefitsDbContext db) => _db = db;

    private Guid TenantId => Guid.TryParse(User.FindFirst(TenantClaim)?.Value, out var t) ? t : Guid.Empty;
    private Guid CurrentUserId
    {
        get
        {
            var v = User.FindFirst("sub")?.Value
                    ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            return Guid.TryParse(v, out var id) ? id : Guid.Empty;
        }
    }
    private static BenefitEnrollmentDto ToDto(BenefitEnrollment e) =>
        new(e.Id, e.EmployeeId, e.BenefitPlanId, e.BenefitPlanName, e.CoverageStartDate, (int)e.Status);

    /// <summary>Enrols the current user in a plan. Employee / Manager.</summary>
    [HttpPost]
    [Authorize(Roles = "Employee,Manager")]
    public async Task<ActionResult<BenefitEnrollmentDto>> Enroll(EnrollRequest req, CancellationToken ct)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == req.BenefitPlanId && p.TenantId == TenantId && p.IsActive, ct);
        if (plan is null) return BadRequest(new { title = "The benefit plan does not exist or is not open for enrolment." });

        // Cosmos can't translate Any(); fetch this user's enrolments (partition-key query) and check client-side.
        var myEnrollments = await _db.Enrollments.AsNoTracking()
            .Where(e => e.TenantId == TenantId && e.EmployeeId == CurrentUserId).ToListAsync(ct);
        if (myEnrollments.Any(e => e.BenefitPlanId == plan.Id && e.Status == EnrollmentStatus.Active))
            return Conflict(new { title = "Already actively enrolled in this plan." });

        var enrollment = new BenefitEnrollment
        {
            TenantId = TenantId, EmployeeId = CurrentUserId, BenefitPlanId = plan.Id,
            BenefitPlanName = plan.Name, CoverageStartDate = req.CoverageStartDate,
        };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetMine), null, ToDto(enrollment));
    }

    /// <summary>Cancels one of the current user's enrolments.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Employee,Manager")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var enrollment = await _db.Enrollments.FirstOrDefaultAsync(
            e => e.Id == id && e.TenantId == TenantId && e.EmployeeId == CurrentUserId, ct);
        if (enrollment is null) return NotFound();
        enrollment.Status = EnrollmentStatus.Cancelled;
        enrollment.CancelledAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Lists the current user's enrolments.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<BenefitEnrollmentDto>>> GetMine(CancellationToken ct)
    {
        // Order client-side: Cosmos can't reliably translate OrderBy over a DateOnly. The per-user
        // enrolment set is tiny, so this is cheap.
        var items = await _db.Enrollments.AsNoTracking()
            .Where(e => e.TenantId == TenantId && e.EmployeeId == CurrentUserId)
            .ToListAsync(ct);
        return Ok(items.OrderByDescending(e => e.CoverageStartDate).Select(ToDto).ToList());
    }
}
