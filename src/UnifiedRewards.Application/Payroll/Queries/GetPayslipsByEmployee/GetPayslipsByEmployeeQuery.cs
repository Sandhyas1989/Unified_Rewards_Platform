using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Payroll.Dtos;

namespace UnifiedRewards.Application.Payroll.Queries.GetPayslipsByEmployee;

public sealed record GetPayslipsByEmployeeQuery(Guid EmployeeId) : IRequest<IReadOnlyList<PayslipDto>>;

public sealed class GetPayslipsByEmployeeQueryHandler
    : IRequestHandler<GetPayslipsByEmployeeQuery, IReadOnlyList<PayslipDto>>
{
    private readonly IApplicationDbContext _db;

    public GetPayslipsByEmployeeQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<PayslipDto>> Handle(GetPayslipsByEmployeeQuery request, CancellationToken cancellationToken)
    {
        return await _db.Payslips
            .AsNoTracking()
            .Where(p => p.EmployeeId == request.EmployeeId)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Select(p => new PayslipDto(
                p.Id, p.EmployeeId, p.Year, p.Month, p.GrossMonthly, p.TotalDeductionsMonthly, p.NetMonthly, p.GeneratedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
