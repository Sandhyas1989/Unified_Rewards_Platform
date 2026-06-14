using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.BenefitsCatalogue.Domain;
using UnifiedRewards.BenefitsCatalogue.Persistence;

namespace UnifiedRewards.BenefitsCatalogue.Controllers;

[ApiController]
[Route("api/benefit-plans")]
[Authorize]
public sealed class BenefitPlansController : ControllerBase
{
    public const string TenantClaim = "tenant_id";
    private readonly BenefitsDbContext _db;
    public BenefitPlansController(BenefitsDbContext db) => _db = db;

    private Guid TenantId => Guid.TryParse(User.FindFirst(TenantClaim)?.Value, out var t) ? t : Guid.Empty;
    private static BenefitPlanDto ToDto(BenefitPlan p) => new(p.Id, p.Name, p.Description, (int)p.Category, p.MonthlyCost, p.IsActive);

    /// <summary>Lists benefit plans (paged), optionally filtered by category / active. Any authenticated user.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<BenefitPlanDto>>> Get(
        [FromQuery] int? category, [FromQuery] bool activeOnly = true, [FromQuery] int? page = null, [FromQuery] int? pageSize = null, CancellationToken ct = default)
    {
        var query = _db.Plans.AsNoTracking().Where(p => p.TenantId == TenantId);
        if (activeOnly) query = query.Where(p => p.IsActive);
        if (category is not null) query = query.Where(p => (int)p.Category == category);
        var p2 = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 25, 1, 200);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(p => p.Name).Skip((p2 - 1) * size).Take(size).ToListAsync(ct);
        return Ok(new PagedResult<BenefitPlanDto>(items.Select(ToDto).ToList(), p2, size, total));
    }

    /// <summary>Creates a benefit plan. HR Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "HrAdmin")]
    public async Task<ActionResult<BenefitPlanDto>> Create(CreatePlanRequest req, CancellationToken ct)
    {
        if (await _db.Plans.AnyAsync(p => p.TenantId == TenantId && p.Name == req.Name.Trim(), ct))
            return Conflict(new { title = "A benefit plan with this name already exists." });

        var plan = new BenefitPlan
        {
            TenantId = TenantId, Name = req.Name.Trim(), Description = req.Description.Trim(),
            Category = (BenefitCategory)req.Category, MonthlyCost = req.MonthlyCost,
        };
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), null, ToDto(plan));
    }
}
