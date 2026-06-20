using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.ReportingCompliance.Persistence;

namespace UnifiedRewards.ReportingCompliance.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "HrAdmin,Finance")]
public sealed class AuditController : ControllerBase
{
    public const string TenantClaim = "tenant_id";
    private Guid TenantId => Guid.TryParse(User.FindFirst(TenantClaim)?.Value, out var t) ? t : Guid.Empty;

    private readonly ReportingDbContext _db;
    public AuditController(ReportingDbContext db) => _db = db;

    /// <summary>Returns the immutable claim audit trail for the caller's tenant, newest first.
    /// Optionally filter by a specific claim with ?claimId=.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditEntryDto>>> Get(
        [FromQuery] Guid? claimId, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        var q = _db.AuditEntries.AsNoTracking().Where(a => a.TenantId == TenantId);
        if (claimId is not null) q = q.Where(a => a.ClaimId == claimId);

        var p = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 50, 1, 200);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.OccurredAtUtc).Skip((p - 1) * size).Take(size).ToListAsync(ct);

        return Ok(new PagedResult<AuditEntryDto>(
            items.Select(a => new AuditEntryDto(
                a.Id, a.EventType, a.ClaimId, a.ActorId, a.Amount, a.Notes, a.OccurredAtUtc)).ToList(),
            p, size, total));
    }
}
