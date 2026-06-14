using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiedRewards.Application.Reporting.Dtos;
using UnifiedRewards.Application.Reporting.Queries.ExportClaimsReport;
using UnifiedRewards.Application.Reporting.Queries.GetAuditEntries;
using UnifiedRewards.Application.Reporting.Queries.GetDashboardReport;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public sealed class ReportingController : ApiControllerBase
{
    private const string ReportViewers = nameof(UserRole.HrAdmin) + "," + nameof(UserRole.Finance);
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly ISender _sender;

    public ReportingController(ISender sender) => _sender = sender;

    /// <summary>Cross-module operational dashboard (LINQ aggregations). HR Admin / Finance.</summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = ReportViewers)]
    public async Task<ActionResult<DashboardReportDto>> Dashboard(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetDashboardReportQuery(), cancellationToken));

    /// <summary>Exports a claims report as an .xlsx workbook. HR Admin / Finance.</summary>
    [HttpGet("claims/export")]
    [Authorize(Roles = ReportViewers)]
    public async Task<IActionResult> ExportClaims([FromQuery] ClaimStatus? status, CancellationToken cancellationToken)
    {
        var file = await _sender.Send(new ExportClaimsReportQuery(status), cancellationToken);
        return File(file.Content, XlsxContentType, file.FileName);
    }

    /// <summary>Queries the audit trail. HR Admin only.</summary>
    [HttpGet("audit")]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<IActionResult> Audit(
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? action,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetAuditEntriesQuery(userId, fromUtc, toUtc, action, page, pageSize), cancellationToken));
}
