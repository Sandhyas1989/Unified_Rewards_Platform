using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Benefits.Dtos;
using UnifiedRewards.Application.Common.Interfaces;

namespace UnifiedRewards.Application.Benefits.Queries.GetEnrollmentsByEmployee;

public sealed record GetEnrollmentsByEmployeeQuery(Guid EmployeeId) : IRequest<IReadOnlyList<BenefitEnrollmentDto>>;

public sealed class GetEnrollmentsByEmployeeQueryHandler
    : IRequestHandler<GetEnrollmentsByEmployeeQuery, IReadOnlyList<BenefitEnrollmentDto>>
{
    private readonly IApplicationDbContext _db;

    public GetEnrollmentsByEmployeeQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<BenefitEnrollmentDto>> Handle(
        GetEnrollmentsByEmployeeQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.BenefitEnrollments
            .AsNoTracking()
            .Where(e => e.EmployeeId == request.EmployeeId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Select(e => new BenefitEnrollmentDto(
                e.Id,
                e.EmployeeId,
                e.BenefitPlanId,
                e.BenefitPlan!.Name,
                e.CoverageStartDate,
                e.Status))
            .ToListAsync(cancellationToken);
    }
}
