using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.CompensationRules.Domain;
using UnifiedRewards.CompensationRules.Persistence;
using UnifiedRewards.CompensationRules.Rules;

namespace UnifiedRewards.CompensationRules.Controllers;

[ApiController]
[Route("api/compensation")]
[Authorize]
public sealed class CompensationController : ControllerBase
{
    public const string TenantClaim = "tenant_id";
    private readonly CompensationDbContext _db;
    private readonly ICompensationCalculator _calculator;

    public CompensationController(CompensationDbContext db, ICompensationCalculator calculator)
    {
        _db = db;
        _calculator = calculator;
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

    private static CompensationStructureDto ToDto(CompensationStructure s) => new(
        s.Id, s.EmployeeId, (int)s.Grade, s.AnnualBasic, s.EffectiveFrom, (int)s.Status,
        s.GrossAnnual, s.TotalDeductions, s.NetAnnual,
        s.Components.OrderBy(c => c.Type).Select(c => new CompensationComponentDto(c.Name, c.Amount, (int)c.Type)).ToList());

    /// <summary>Generates a rule-based compensation structure (NRules). HR Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "HrAdmin")]
    public async Task<ActionResult<CompensationStructureDto>> Generate(GenerateCompensationRequest req, CancellationToken ct)
    {
        var breakdown = _calculator.Calculate(req.AnnualBasic, (GradeBand)req.Grade);
        var structure = new CompensationStructure
        {
            TenantId = TenantId,
            EmployeeId = req.EmployeeId,
            Grade = (GradeBand)req.Grade,
            AnnualBasic = req.AnnualBasic,
            EffectiveFrom = req.EffectiveFrom,
            GrossAnnual = breakdown.GrossAnnual,
            TotalDeductions = breakdown.TotalDeductions,
            NetAnnual = breakdown.NetAnnual,
            Components = breakdown.Lines.Select(l => new CompensationComponent { Name = l.Name, Amount = l.Amount, Type = l.Type }).ToList(),
        };
        _db.Structures.Add(structure);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = structure.Id }, ToDto(structure));
    }

    /// <summary>Approves a draft compensation structure. HR Admin / Finance.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "HrAdmin,Finance")]
    public async Task<ActionResult<CompensationStructureDto>> Approve(Guid id, CancellationToken ct)
    {
        var s = await _db.Structures.Include(x => x.Components).FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId, ct);
        if (s is null) return NotFound();
        s.Approve();
        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(s));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompensationStructureDto>> GetById(Guid id, CancellationToken ct)
    {
        var s = await _db.Structures.AsNoTracking().Include(x => x.Components).FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId, ct);
        return s is null ? NotFound() : Ok(ToDto(s));
    }

    /// <summary>Lists a given employee's compensation history.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompensationStructureDto>>> GetByEmployee([FromQuery] Guid employeeId, CancellationToken ct)
    {
        var list = await _db.Structures.AsNoTracking().Include(x => x.Components)
            .Where(x => x.TenantId == TenantId && x.EmployeeId == employeeId)
            .OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct);
        return Ok(list.Select(ToDto).ToList());
    }

    /// <summary>Lists the current user's own compensation history.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<CompensationStructureDto>>> GetMine(CancellationToken ct)
    {
        var list = await _db.Structures.AsNoTracking().Include(x => x.Components)
            .Where(x => x.TenantId == TenantId && x.EmployeeId == CurrentUserId)
            .OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct);
        return Ok(list.Select(ToDto).ToList());
    }
}
