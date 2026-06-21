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
        // Fetch this tenant's catalogue by partition key (Cosmos-friendly), then filter/sort/page in
        // memory. The catalogue is small and per-tenant, and Cosmos can't translate offset pagination
        // (Skip/Take) or the enum-cast predicate, so client-side evaluation is correct and inexpensive.
        var plans = await _db.Plans.AsNoTracking().Where(p => p.TenantId == TenantId).ToListAsync(ct);
        IEnumerable<BenefitPlan> filtered = plans;
        if (activeOnly) filtered = filtered.Where(p => p.IsActive);
        if (category is not null) filtered = filtered.Where(p => (int)p.Category == category);
        var ordered = filtered.OrderBy(p => p.Name).ToList();
        var p2 = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 25, 1, 200);
        var items = ordered.Skip((p2 - 1) * size).Take(size).Select(ToDto).ToList();
        return Ok(new PagedResult<BenefitPlanDto>(items, p2, size, ordered.Count));
    }

    /// <summary>Creates a benefit plan. HR Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "HrAdmin")]
    public async Task<ActionResult<BenefitPlanDto>> Create(CreatePlanRequest req, CancellationToken ct)
    {
        // Cosmos can't translate the Any() aggregate; fetch this tenant's plans (partition-key query)
        // and check the name client-side.
        var name = req.Name.Trim();
        var tenantPlans = await _db.Plans.AsNoTracking().Where(p => p.TenantId == TenantId).ToListAsync(ct);
        if (tenantPlans.Any(p => p.Name == name))
            return Conflict(new { title = "A benefit plan with this name already exists." });

        var plan = new BenefitPlan
        {
            TenantId = TenantId, Name = name, Description = req.Description.Trim(),
            Category = (BenefitCategory)req.Category, MonthlyCost = req.MonthlyCost,
        };
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), null, ToDto(plan));
    }
}
