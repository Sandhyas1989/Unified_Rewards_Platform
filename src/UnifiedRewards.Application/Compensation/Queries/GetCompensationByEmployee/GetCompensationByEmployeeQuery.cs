using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Compensation.Dtos;

namespace UnifiedRewards.Application.Compensation.Queries.GetCompensationByEmployee;

public sealed record GetCompensationByEmployeeQuery(Guid EmployeeId)
    : IRequest<IReadOnlyList<CompensationStructureDto>>;

public sealed class GetCompensationByEmployeeQueryHandler
    : IRequestHandler<GetCompensationByEmployeeQuery, IReadOnlyList<CompensationStructureDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCompensationByEmployeeQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CompensationStructureDto>> Handle(
        GetCompensationByEmployeeQuery request,
        CancellationToken cancellationToken)
    {
        var structures = await _db.CompensationStructures
            .AsNoTracking()
            .Include(s => s.Components)
            .Where(s => s.EmployeeId == request.EmployeeId)
            .OrderByDescending(s => s.EffectiveFrom)
            .ToListAsync(cancellationToken);

        return structures.Select(s => s.ToDto()).ToList();
    }
}
