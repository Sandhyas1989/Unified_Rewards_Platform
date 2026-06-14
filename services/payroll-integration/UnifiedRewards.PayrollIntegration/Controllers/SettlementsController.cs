using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.PayrollIntegration.Domain;
using UnifiedRewards.PayrollIntegration.Persistence;
using UnifiedRewards.PayrollIntegration.Processing;

namespace UnifiedRewards.PayrollIntegration.Controllers;

[ApiController]
[Route("api/settlements")]
[Authorize]
public sealed class SettlementsController : ControllerBase
{
    public const string TenantClaim = "tenant_id";

    private readonly PayrollDbContext _db;
    private readonly SettlementQueue _queue;

    public SettlementsController(PayrollDbContext db, SettlementQueue queue)
    {
        _db = db;
        _queue = queue;
    }

    private Guid TenantId => Guid.TryParse(User.FindFirst(TenantClaim)?.Value, out var t) ? t : Guid.Empty;

    private static SettlementDto ToDto(SettlementRequest s) =>
        new(s.Id, s.EmployeeId, s.Amount, s.Reference, s.Status, s.Attempts, s.PayrollConfirmation, s.LastError, s.RequestedAtUtc, s.CompletedAtUtc);

    /// <summary>Queues an asynchronous payroll settlement. Returns 202 with a status URL. Finance only.</summary>
    [HttpPost]
    [Authorize(Roles = "Finance")]
    public async Task<ActionResult<SettlementDto>> RequestSettlement(RequestSettlementRequest req, CancellationToken ct)
    {
        var settlement = new SettlementRequest { TenantId = TenantId, EmployeeId = req.EmployeeId, Amount = req.Amount };
        settlement.Reference = $"SET-{settlement.Id:N}";
        _db.Settlements.Add(settlement);
        await _db.SaveChangesAsync(ct);
        await _queue.EnqueueAsync(settlement.Id, ct);   // async processing (resilient push happens in the worker)
        return AcceptedAtAction(nameof(GetById), new { id = settlement.Id }, ToDto(settlement));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SettlementDto>> GetById(Guid id, CancellationToken ct)
    {
        var s = await _db.Settlements.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId, ct);
        return s is null ? NotFound() : Ok(ToDto(s));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SettlementDto>>> Get([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        var query = _db.Settlements.AsNoTracking().Where(s => s.TenantId == TenantId);
        var p = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 25, 1, 200);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(s => s.RequestedAtUtc).Skip((p - 1) * size).Take(size).ToListAsync(ct);
        return Ok(new PagedResult<SettlementDto>(items.Select(ToDto).ToList(), p, size, total));
    }
}
