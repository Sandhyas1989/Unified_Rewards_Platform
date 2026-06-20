using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Messaging;
using UnifiedRewards.Messaging.Events;
using UnifiedRewards.ReimbursementWorkflow.Domain;
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
    private readonly IEventBus _bus;

    public ClaimsController(ReimbursementDbContext db, IEventBus bus)
    {
        _db = db;
        _bus = bus;
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
        c.Id, c.EmployeeId, (int)c.Type, c.Amount, c.CurrencyCode, c.Description, (int)c.Status, c.ReviewerId, c.DecisionNotes,
        c.SettlementReference, c.SubmittedAtUtc, c.DecisionAtUtc, c.SettledAtUtc,
        c.History.OrderBy(h => h.OccurredAtUtc).Select(h => new ClaimTransitionDto((int?)h.FromStatus, (int)h.ToStatus, h.ActorId, h.Notes, h.OccurredAtUtc)).ToList());

    [HttpPost]
    [Authorize(Roles = "Employee,Manager")]
    public async Task<ActionResult<ClaimDto>> Submit(SubmitClaimRequest req, CancellationToken ct)
    {
        var claim = Claim.Submit(TenantId, CurrentUserId, (ClaimType)req.Type, req.Amount, req.Description, req.CurrencyCode);
        _db.Claims.Add(claim);
        // Stage the event in the SAME unit of work as the claim (transactional outbox), then commit both.
        await _bus.PublishAsync(new ClaimSubmitted(claim.Id, claim.EmployeeId, (int)claim.Type, claim.Amount, claim.SubmittedAtUtc), TenantId, ct);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = claim.Id }, ToDto(claim));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Reviewers)]
    public Task<ActionResult<ClaimDto>> Approve(Guid id, [FromBody] DecisionRequest? body, CancellationToken ct)
        => Decide(id, c => c.Approve(CurrentUserId, body?.Notes),
                  c => new ClaimApproved(c.Id, c.EmployeeId, CurrentUserId, c.Amount, DateTime.UtcNow), ct);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Reviewers)]
    public Task<ActionResult<ClaimDto>> Reject(Guid id, [FromBody] DecisionRequest? body, CancellationToken ct)
        => Decide(id, c => c.Reject(CurrentUserId, body?.Notes),
                  c => new ClaimRejected(c.Id, c.EmployeeId, CurrentUserId, body?.Notes, DateTime.UtcNow), ct);

    private async Task<ActionResult<ClaimDto>> Decide(Guid id, Action<Claim> action, Func<Claim, object> eventFactory, CancellationToken ct)
    {
        var claim = await _db.Claims.Include(c => c.History).FirstOrDefaultAsync(c => c.Id == id && c.TenantId == TenantId, ct);
        if (claim is null) return NotFound();
        try { action(claim); }
        catch (InvalidClaimTransitionException ex) { return Conflict(new { title = ex.Message }); }
        await _bus.PublishAsync(eventFactory(claim), TenantId, ct);   // staged in the same transaction
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { title = "The claim was modified by another request. Please reload and retry." });
        }
        return Ok(ToDto(claim));
    }

    /// <summary>Requests settlement of an approved claim via the event-driven saga: publishes
    /// SettlementRequested and returns 202. Payroll settles asynchronously and the claim is closed when
    /// SettlementCompleted arrives (see SettlementCompletedHandler). Finance only.</summary>
    [HttpPost("{id:guid}/settle")]
    [Authorize(Roles = "Finance")]
    public async Task<ActionResult<ClaimDto>> Settle(Guid id, CancellationToken ct)
    {
        var claim = await _db.Claims.Include(c => c.History).FirstOrDefaultAsync(c => c.Id == id && c.TenantId == TenantId, ct);
        if (claim is null) return NotFound();
        if (claim.Status != ClaimStatus.Approved) return Conflict(new { title = "Only an approved claim can be settled." });

        // No more synchronous call + polling — hand off to Payroll over the bus (staged in the outbox).
        await _bus.PublishAsync(new SettlementRequested(claim.Id, claim.EmployeeId, claim.Amount, DateTime.UtcNow), TenantId, ct);
        await _db.SaveChangesAsync(ct);
        return Accepted(ToDto(claim));   // 202: claim stays Approved; becomes Settled via the saga
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
