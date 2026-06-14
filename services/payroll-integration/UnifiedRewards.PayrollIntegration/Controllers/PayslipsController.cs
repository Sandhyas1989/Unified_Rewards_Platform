using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.PayrollIntegration.Domain;
using UnifiedRewards.PayrollIntegration.Persistence;

namespace UnifiedRewards.PayrollIntegration.Controllers;

[ApiController]
[Route("api/payslips")]
[Authorize]
public sealed class PayslipsController : ControllerBase
{
    public const string TenantClaim = "tenant_id";

    private readonly PayrollDbContext _db;

    public PayslipsController(PayrollDbContext db) => _db = db;

    private Guid TenantId => Guid.TryParse(User.FindFirst(TenantClaim)?.Value, out var t) ? t : Guid.Empty;

    private static PayslipDto ToDto(Payslip p) =>
        new(p.Id, p.EmployeeId, p.Year, p.Month, p.GrossMonthly, p.TotalDeductionsMonthly, p.NetMonthly, p.GeneratedAtUtc);

    /// <summary>Generates/stores a monthly payslip (idempotent per employee+period). Finance / HR Admin.
    /// Monthly figures originate from the Compensation Rules service (passed in / via events in prod).</summary>
    [HttpPost]
    [Authorize(Roles = "Finance,HrAdmin")]
    public async Task<ActionResult<PayslipDto>> Generate(GeneratePayslipRequest req, CancellationToken ct)
    {
        var existing = await _db.Payslips.FirstOrDefaultAsync(
            p => p.TenantId == TenantId && p.EmployeeId == req.EmployeeId && p.Year == req.Year && p.Month == req.Month, ct);
        if (existing is not null) return Ok(ToDto(existing));

        var payslip = new Payslip
        {
            TenantId = TenantId,
            EmployeeId = req.EmployeeId,
            Year = req.Year,
            Month = req.Month,
            GrossMonthly = req.GrossMonthly,
            TotalDeductionsMonthly = req.TotalDeductionsMonthly,
            NetMonthly = req.NetMonthly,
        };
        _db.Payslips.Add(payslip);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { employeeId = req.EmployeeId }, ToDto(payslip));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PayslipDto>>> Get(
        [FromQuery] Guid? employeeId, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        var query = _db.Payslips.AsNoTracking().Where(p => p.TenantId == TenantId);
        if (employeeId is not null) query = query.Where(p => p.EmployeeId == employeeId);
        var p2 = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 25, 1, 200);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).Skip((p2 - 1) * size).Take(size).ToListAsync(ct);
        return Ok(new PagedResult<PayslipDto>(items.Select(ToDto).ToList(), p2, size, total));
    }
}
