using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Reporting.Dtos;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Reporting.Queries.ExportClaimsReport;

public sealed record ExportClaimsReportQuery(ClaimStatus? Status = null) : IRequest<ExcelFile>;

public sealed class ExportClaimsReportQueryHandler : IRequestHandler<ExportClaimsReportQuery, ExcelFile>
{
    private readonly IApplicationDbContext _db;
    private readonly IReportExporter _exporter;

    public ExportClaimsReportQueryHandler(IApplicationDbContext db, IReportExporter exporter)
    {
        _db = db;
        _exporter = exporter;
    }

    public async Task<ExcelFile> Handle(ExportClaimsReportQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Claims.AsNoTracking();
        if (request.Status is not null)
        {
            query = query.Where(c => c.Status == request.Status);
        }

        var claims = await query
            .OrderByDescending(c => c.SubmittedAtUtc)
            .Select(c => new { c.Id, c.Type, c.Amount, c.Status, c.SubmittedAtUtc, c.SettledAtUtc })
            .ToListAsync(cancellationToken);

        var rows = claims
            .Select(c => new ClaimReportRow(
                c.Id, c.Type.ToString(), c.Amount, c.Status.ToString(), c.SubmittedAtUtc, c.SettledAtUtc))
            .ToList();

        var bytes = _exporter.BuildClaimsWorkbook(rows);
        return new ExcelFile(bytes, $"claims-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx");
    }
}
