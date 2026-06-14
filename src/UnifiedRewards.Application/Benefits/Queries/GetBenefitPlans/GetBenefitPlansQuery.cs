using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Benefits.Dtos;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Benefits.Queries.GetBenefitPlans;

public sealed record GetBenefitPlansQuery(BenefitCategory? Category = null, bool ActiveOnly = true)
    : IRequest<IReadOnlyList<BenefitPlanDto>>;

public sealed class GetBenefitPlansQueryHandler : IRequestHandler<GetBenefitPlansQuery, IReadOnlyList<BenefitPlanDto>>
{
    private readonly IApplicationDbContext _db;

    public GetBenefitPlansQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<BenefitPlanDto>> Handle(GetBenefitPlansQuery request, CancellationToken cancellationToken)
    {
        var query = _db.BenefitPlans.AsNoTracking();

        if (request.ActiveOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        if (request.Category is not null)
        {
            query = query.Where(p => p.Category == request.Category);
        }

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new BenefitPlanDto(p.Id, p.Name, p.Description, p.Category, p.MonthlyCost, p.IsActive))
            .ToListAsync(cancellationToken);
    }
}
