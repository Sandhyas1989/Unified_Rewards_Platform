using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.ReimbursementWorkflow.Domain;
using UnifiedRewards.ReimbursementWorkflow.Integration;
using UnifiedRewards.ReimbursementWorkflow.Persistence;

namespace UnifiedRewards.ReimbursementWorkflow.Controllers;

[ApiController]
[Route("api/claims")]
[Authorize]
public sealed class ClaimsController : ControllerBase
{
    private const string Reviewers = "Manager,Finance,HrAdmin";
    public const string TenantClaim = "tenant_id";

    private readonly ReimbursementDbContext _db;
    private readonly PayrollClient _payroll;

    public ClaimsController(ReimbursementDbContext db, PayrollClient payroll)
    {
        _db = db;
        _payroll = payroll;
    }

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

    private static ClaimDto ToDto(Claim c) => new(
        c.Id, c.EmployeeId, (int)c.Type, c.Amount, c.Description, (int)c.Status, c.ReviewerId, c.DecisionNotes,
        c.SettlementReference, c.SubmittedAtUtc, c.DecisionAtUtc, c.SettledAtUtc,
        c.History.OrderBy(h => h.OccurredAtUtc).Select(h => new ClaimTransitionDto((int?)h.FromStatus, (int)h.ToStatus, h.ActorId, h.Notes, h.OccurredAtUtc)).ToList());

    [HttpPost]
    [Authorize(Roles = "Employee,Manager")]
    public async Task<ActionResult<ClaimDto>> Submit(SubmitClaimRequest req, CancellationToken ct)
    {
        var claim = Claim.Submit(TenantId, CurrentUserId, (ClaimType)req.Type, req.Amount, req.Description);
        _db.Claims.Add(claim);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = claim.Id }, ToDto(claim));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Reviewers)]
    public Task<ActionResult<ClaimDto>> Approve(Guid id, [FromBody] DecisionRequest? body, CancellationToken ct)
        => Decide(id, c => c.Approve(CurrentUserId, body?.Notes), ct);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Reviewers)]
    public Task<ActionResult<ClaimDto>> Reject(Guid id, [FromBody] DecisionRequest? body, CancellationToken ct)
        => Decide(id, c => c.Reject(CurrentUserId, body?.Notes), ct);

    private async Task<ActionResult<ClaimDto>> Decide(Guid id, Action<Claim> action, CancellationToken ct)
    {
        var claim = await _db.Claims.Include(c => c.History).FirstOrDefaultAsync(c => c.Id == id && c.TenantId == TenantId, ct);
        if (claim is null) return NotFound();
        try { action(claim); }
        catch (InvalidClaimTransitionException ex) { return Conflict(new { title = ex.Message }); }
        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(claim));
    }

    /// <summary>Settles an approved claim by ORCHESTRATING the Payroll Integration service
    /// (request a settlement, wait for it to process, then close the claim). Finance only.</summary>
    [HttpPost("{id:guid}/settle")]
    [Authorize(Roles = "Finance")]
    public async Task<ActionResult<ClaimDto>> Settle(Guid id, CancellationToken ct)
    {
        var claim = await _db.Claims.Include(c => c.History).FirstOrDefaultAsync(c => c.Id == id && c.TenantId == TenantId, ct);
        if (claim is null) return NotFound();
        if (claim.Status != ClaimStatus.Approved) return Conflict(new { title = "Only an approved claim can be settled." });

        var auth = Request.Headers.Authorization.ToString();
        var settlement = await _payroll.RequestSettlementAsync(claim.EmployeeId, claim.Amount, auth, ct);
        if (settlement is null) return StatusCode(502, new { title = "Payroll service did not accept the settlement." });

        // Wait for the asynchronous settlement to reach a terminal state (2=Succeeded, 3=Failed).
        int? status = null;
        for (var i = 0; i < 15; i++)
        {
            status = await _payroll.GetSettlementStatusAsync(settlement.Value.Id, auth, ct);
            if (status is >= 2) break;
            await Task.Delay(400, ct);
        }

        if (status != 2) return StatusCode(502, new { title = $"Settlement did not succeed (status {status}).", settlement.Value.Reference });

        try { claim.Settle(CurrentUserId, settlement.Value.Reference); }
        catch (InvalidClaimTransitionException ex) { return Conflict(new { title = ex.Message }); }
        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(claim));
    }

    [HttpGet]
    [Authorize(Roles = Reviewers)]
    public async Task<ActionResult<PagedResult<ClaimDto>>> Get([FromQuery] int? status, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        var query = _db.Claims.AsNoTracking().Include(c => c.History).Where(c => c.TenantId == TenantId);
        if (status is not null) query = query.Where(c => (int)c.Status == status);
        var p = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 25, 1, 200);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(c => c.SubmittedAtUtc).Skip((p - 1) * size).Take(size).ToListAsync(ct);
        return Ok(new PagedResult<ClaimDto>(items.Select(ToDto).ToList(), p, size, total));
    }

    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<ClaimDto>>> GetMine(CancellationToken ct)
    {
        var items = await _db.Claims.AsNoTracking().Include(c => c.History)
            .Where(c => c.TenantId == TenantId && c.EmployeeId == CurrentUserId)
            .OrderByDescending(c => c.SubmittedAtUtc).ToListAsync(ct);
        return Ok(items.Select(ToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Reviewers)]
    public async Task<ActionResult<ClaimDto>> GetById(Guid id, CancellationToken ct)
    {
        var claim = await _db.Claims.AsNoTracking().Include(c => c.History).FirstOrDefaultAsync(c => c.Id == id && c.TenantId == TenantId, ct);
        return claim is null ? NotFound() : Ok(ToDto(claim));
    }
}
