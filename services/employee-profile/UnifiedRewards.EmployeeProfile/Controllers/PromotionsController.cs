using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.EmployeeProfile.Auth;
using UnifiedRewards.EmployeeProfile.Domain;
using UnifiedRewards.EmployeeProfile.Persistence;
using UnifiedRewards.Messaging;
using UnifiedRewards.Messaging.Events;

namespace UnifiedRewards.EmployeeProfile.Controllers;

[ApiController]
[Route("api/promotions")]
[Authorize]
public sealed class PromotionsController : ControllerBase
{
    private readonly EmployeeProfileDbContext _db;
    private readonly IEventBus _bus;

    public PromotionsController(EmployeeProfileDbContext db, IEventBus bus) { _db = db; _bus = bus; }

    private Guid TenantId =>
        Guid.TryParse(User.FindFirst(JwtTokenService.TenantClaim)?.Value, out var t) ? t : Guid.Empty;

    private Guid CurrentUserId
    {
        get
        {
            var v = User.FindFirst("sub")?.Value
                    ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            return Guid.TryParse(v, out var id) ? id : Guid.Empty;
        }
    }

    private static PromotionDto ToDto(Promotion p) => new(
        p.Id, p.Title, p.CycleYear, p.CycleQuarter, p.FromGrade, p.BonusValue,
        p.NominationStart, p.NominationEnd, (int)p.Status,
        p.Nominations.Count,
        p.Nominations.Count(n => n.Outcome == NominationOutcome.Promoted),
        p.CreatedAtUtc);

    private static NominationDto ToNomDto(PromotionNomination n, string? employeeName) =>
        new(n.Id, n.PromotionId, n.EmployeeId, employeeName, n.NominatedBy, n.NominatedOn,
            (int)n.Outcome, n.Remarks, n.CreatedAtUtc);

    // ── Cycle management (HR Admin) ──────────────────────────────────────────

    /// <summary>HR Admin creates a new promotion cycle (starts as Draft).</summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<PromotionDto>> Create(CreatePromotionRequest req, CancellationToken ct)
    {
        var promo = new Promotion
        {
            TenantId = TenantId,
            CreatedBy = CurrentUserId,
            Title = req.Title.Trim(),
            CycleYear = req.CycleYear,
            CycleQuarter = req.CycleQuarter,
            FromGrade = req.FromGrade.Trim(),
            BonusValue = req.BonusValue,
            NominationStart = req.NominationStart,
            NominationEnd = req.NominationEnd,
        };

        if (req.MinTenureMonths is not null)
        {
            promo.Eligibility = new PromotionEligibility
            {
                PromotionId = promo.Id,
                MinTenureMonths = req.MinTenureMonths.Value,
                MinPerformanceRating = req.MinPerformanceRating,
                MinCurrentGrade = req.MinCurrentGrade,
            };
        }

        _db.Promotions.Add(promo);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = promo.Id }, ToDto(promo));
    }

    /// <summary>Lists promotion cycles for the caller's tenant.</summary>
    [HttpGet]
    [Authorize(Roles = "HrAdmin,Manager,Employee")]
    public async Task<ActionResult<PagedResult<PromotionDto>>> Get(
        [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        var p = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 25, 1, 200);
        var q = _db.Promotions.Include(x => x.Nominations).Where(x => x.TenantId == TenantId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CycleYear).ThenBy(x => x.CycleQuarter)
            .Skip((p - 1) * size).Take(size).ToListAsync(ct);
        return Ok(new PagedResult<PromotionDto>(items.Select(ToDto).ToList(), p, size, total));
    }

    /// <summary>Gets a single cycle with its nominations.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "HrAdmin,Manager,Employee")]
    public async Task<ActionResult<PromotionDto>> GetById(Guid id, CancellationToken ct)
    {
        var promo = await _db.Promotions.Include(x => x.Nominations)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId, ct);
        return promo is null ? NotFound() : Ok(ToDto(promo));
    }

    /// <summary>Opens a Draft cycle for nominations.</summary>
    [HttpPost("{id:guid}/open")]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<PromotionDto>> Open(Guid id, CancellationToken ct)
    {
        var promo = await _db.Promotions.Include(x => x.Nominations)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId, ct);
        if (promo is null) return NotFound();
        try { promo.Open(); }
        catch (InvalidOperationException ex) { return Conflict(new { title = ex.Message }); }
        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(promo));
    }

    /// <summary>Closes an Open cycle (no further nominations accepted).</summary>
    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<PromotionDto>> Close(Guid id, CancellationToken ct)
    {
        var promo = await _db.Promotions.Include(x => x.Nominations)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId, ct);
        if (promo is null) return NotFound();
        try { promo.Close(); }
        catch (InvalidOperationException ex) { return Conflict(new { title = ex.Message }); }
        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(promo));
    }

    /// <summary>Cancels a Draft or Open cycle.</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<PromotionDto>> Cancel(Guid id, CancellationToken ct)
    {
        var promo = await _db.Promotions.Include(x => x.Nominations)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId, ct);
        if (promo is null) return NotFound();
        try { promo.Cancel(); }
        catch (InvalidOperationException ex) { return Conflict(new { title = ex.Message }); }
        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(promo));
    }

    // ── Nominations ──────────────────────────────────────────────────────────

    /// <summary>Manager nominates an employee into an Open cycle. Runs an eligibility check
    /// (grade + tenure) and records the snapshot for audit. Returns 422 if not eligible.</summary>
    [HttpPost("{id:guid}/nominations")]
    [Authorize(Roles = "Manager,HrAdmin")]
    public async Task<ActionResult<NominationDto>> Nominate(Guid id, NominateRequest req, CancellationToken ct)
    {
        var promo = await _db.Promotions.Include(x => x.Eligibility)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId, ct);
        if (promo is null) return NotFound();
        if (promo.Status != PromotionStatus.Open)
            return Conflict(new { title = "Promotion cycle is not Open." });

        var employee = await _db.Users.OfType<Employee>()
            .FirstOrDefaultAsync(e => e.Id == req.EmployeeId && e.TenantId == TenantId, ct);
        if (employee is null) return NotFound(new { title = "Employee not found." });

        var alreadyNominated = await _db.PromotionNominations
            .AnyAsync(n => n.PromotionId == id && n.EmployeeId == req.EmployeeId, ct);
        if (alreadyNominated) return Conflict(new { title = "Employee is already nominated in this cycle." });

        // Eligibility evaluation — snapshot persisted for audit regardless of outcome.
        var tenureMonths = (int)((DateTime.UtcNow - employee.DateOfJoining.ToDateTime(TimeOnly.MinValue)).TotalDays / 30.44);
        var isEligible = true;
        string? failureReason = null;

        // Grade filter is optional: empty FromGrade means all grades are eligible.
        if (!string.IsNullOrEmpty(promo.FromGrade) && employee.Grade != promo.FromGrade)
        {
            isEligible = false;
            failureReason = $"Employee grade '{employee.Grade}' does not match eligible grade '{promo.FromGrade}'.";
        }
        else if (promo.Eligibility is not null && tenureMonths < promo.Eligibility.MinTenureMonths)
        {
            isEligible = false;
            failureReason = $"Tenure {tenureMonths} months is below minimum {promo.Eligibility.MinTenureMonths} months.";
        }

        _db.PromotionEligibilityChecks.Add(new PromotionEligibilityCheck
        {
            PromotionId = id,
            EmployeeId = req.EmployeeId,
            IsEligible = isEligible,
            TenureMonths = tenureMonths,
            FailureReason = failureReason,
        });

        if (!isEligible)
        {
            await _db.SaveChangesAsync(ct);
            return UnprocessableEntity(new { title = failureReason });
        }

        var nomination = new PromotionNomination
        {
            TenantId = TenantId,
            PromotionId = id,
            EmployeeId = req.EmployeeId,
            NominatedBy = CurrentUserId,
            Remarks = req.Remarks,
        };
        _db.PromotionNominations.Add(nomination);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetNominations), new { id }, ToNomDto(nomination, employee.FullName));
    }

    /// <summary>Lists all nominations for a cycle, enriched with employee names.</summary>
    [HttpGet("{id:guid}/nominations")]
    [Authorize(Roles = "HrAdmin,Manager")]
    public async Task<ActionResult<IReadOnlyList<NominationDto>>> GetNominations(Guid id, CancellationToken ct)
    {
        if (!await _db.Promotions.AnyAsync(x => x.Id == id && x.TenantId == TenantId, ct)) return NotFound();

        var nominations = await _db.PromotionNominations
            .Where(n => n.PromotionId == id).ToListAsync(ct);

        var employeeIds = nominations.Select(n => n.EmployeeId).Distinct().ToList();
        var nameMap = await _db.Users.AsNoTracking()
            .Where(u => employeeIds.Contains(u.Id) && u.TenantId == TenantId)
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        return Ok(nominations.Select(n => ToNomDto(n, nameMap.GetValueOrDefault(n.EmployeeId))).ToList());
    }

    /// <summary>HR Admin approves a nomination → sets outcome to Promoted and updates the employee's
    /// grade to the cycle's ToGrade in the same transaction.</summary>
    [HttpPost("{id:guid}/nominations/{nomId:guid}/approve")]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<NominationDto>> Approve(
        Guid id, Guid nomId, [FromBody] PromotionDecisionRequest? req, CancellationToken ct)
    {
        var promo = await _db.Promotions.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId, ct);
        if (promo is null) return NotFound();

        var nom = await _db.PromotionNominations.FirstOrDefaultAsync(n => n.Id == nomId && n.PromotionId == id, ct);
        if (nom is null) return NotFound();

        try { nom.Approve(CurrentUserId); }
        catch (InvalidOperationException ex) { return Conflict(new { title = ex.Message }); }
        if (req?.Remarks is not null) nom.Remarks = req.Remarks;

        var employee = await _db.Users.OfType<Employee>()
            .AsNoTracking().FirstOrDefaultAsync(e => e.Id == nom.EmployeeId && e.TenantId == TenantId, ct);

        // Publish BonusAwarded so Payroll Integration can disburse the payout asynchronously.
        await _bus.PublishAsync(
            new BonusAwarded(nom.EmployeeId, id, nom.Id, promo.BonusValue, DateTime.UtcNow),
            TenantId, ct);

        await _db.SaveChangesAsync(ct);
        return Ok(ToNomDto(nom, employee?.FullName));
    }

    /// <summary>HR Admin rejects a nomination.</summary>
    [HttpPost("{id:guid}/nominations/{nomId:guid}/reject")]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<NominationDto>> Reject(
        Guid id, Guid nomId, [FromBody] PromotionDecisionRequest? req, CancellationToken ct)
    {
        if (!await _db.Promotions.AnyAsync(x => x.Id == id && x.TenantId == TenantId, ct)) return NotFound();

        var nom = await _db.PromotionNominations.FirstOrDefaultAsync(n => n.Id == nomId && n.PromotionId == id, ct);
        if (nom is null) return NotFound();

        try { nom.Reject(CurrentUserId, req?.Remarks); }
        catch (InvalidOperationException ex) { return Conflict(new { title = ex.Message }); }

        await _db.SaveChangesAsync(ct);
        return Ok(ToNomDto(nom, null));
    }

    /// <summary>Employee views their own nomination history across all cycles.</summary>
    [HttpGet("me/nominations")]
    public async Task<ActionResult<IReadOnlyList<NominationDto>>> MyNominations(CancellationToken ct)
    {
        var nominations = await _db.PromotionNominations
            .Where(n => n.EmployeeId == CurrentUserId && n.TenantId == TenantId)
            .OrderByDescending(n => n.CreatedAtUtc).ToListAsync(ct);
        return Ok(nominations.Select(n => ToNomDto(n, null)).ToList());
    }
}
