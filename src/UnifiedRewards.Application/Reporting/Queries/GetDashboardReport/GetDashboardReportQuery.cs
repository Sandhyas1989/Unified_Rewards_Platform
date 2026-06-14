using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Reporting.Dtos;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Reporting.Queries.GetDashboardReport;

public sealed record GetDashboardReportQuery : IRequest<DashboardReportDto>;

public sealed class GetDashboardReportQueryHandler : IRequestHandler<GetDashboardReportQuery, DashboardReportDto>
{
    private readonly IApplicationDbContext _db;

    public GetDashboardReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DashboardReportDto> Handle(GetDashboardReportQuery request, CancellationToken cancellationToken)
    {
        // SQLite cannot translate Sum(decimal) to SQL, so the money aggregations are computed
        // client-side on SQLite (tiny dev data) but pushed into the database on SQL Server (scale).
        var isSqlite = _db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

        List<StatusAmountRow> claimsByStatus;
        List<StatusAmountRow> settlementsByStatus;

        if (isSqlite)
        {
            var claims = await _db.Claims.AsNoTracking()
                .Select(c => new { c.Status, c.Amount }).ToListAsync(cancellationToken);
            claimsByStatus = claims.GroupBy(c => c.Status)
                .Select(g => new StatusAmountRow(g.Key.ToString(), g.Count(), g.Sum(x => x.Amount)))
                .OrderBy(r => r.Status).ToList();

            var settlements = await _db.SettlementRequests.AsNoTracking()
                .Select(s => new { s.Status, s.Amount }).ToListAsync(cancellationToken);
            settlementsByStatus = settlements.GroupBy(s => s.Status)
                .Select(g => new StatusAmountRow(g.Key.ToString(), g.Count(), g.Sum(x => x.Amount)))
                .OrderBy(r => r.Status).ToList();
        }
        else
        {
            var claimRows = await _db.Claims.AsNoTracking()
                .GroupBy(c => c.Status)
                .Select(g => new { g.Key, Count = g.Count(), Total = g.Sum(c => c.Amount) })
                .ToListAsync(cancellationToken);
            claimsByStatus = claimRows.OrderBy(r => r.Key)
                .Select(r => new StatusAmountRow(r.Key.ToString(), r.Count, r.Total)).ToList();

            var settlementRows = await _db.SettlementRequests.AsNoTracking()
                .GroupBy(s => s.Status)
                .Select(g => new { g.Key, Count = g.Count(), Total = g.Sum(s => s.Amount) })
                .ToListAsync(cancellationToken);
            settlementsByStatus = settlementRows.OrderBy(r => r.Key)
                .Select(r => new StatusAmountRow(r.Key.ToString(), r.Count, r.Total)).ToList();
        }

        // Count-only aggregations translate on both providers — always done in the database.
        var roleRows = await _db.Users.AsNoTracking()
            .GroupBy(u => u.Role).Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var headcountByRole = roleRows.OrderBy(r => r.Key)
            .Select(r => new CountRow(r.Key.ToString(), r.Count)).ToList();

        var enrollmentRows = await _db.BenefitEnrollments.AsNoTracking()
            .Where(e => e.Status == EnrollmentStatus.Active)
            .GroupBy(e => e.BenefitPlan!.Name).Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var activeEnrollmentsByPlan = enrollmentRows.OrderByDescending(r => r.Count)
            .Select(r => new CountRow(r.Name, r.Count)).ToList();

        var approvedPromotions = await _db.PromotionNominations
            .CountAsync(n => n.Status == PromotionStatus.Approved, cancellationToken);
        var totalPayslips = await _db.Payslips.CountAsync(cancellationToken);

        return new DashboardReportDto(
            DateTime.UtcNow,
            claimsByStatus,
            settlementsByStatus,
            headcountByRole,
            activeEnrollmentsByPlan,
            approvedPromotions,
            totalPayslips);
    }
}
