using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Benefits.Dtos;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Domain.Benefits;

namespace UnifiedRewards.Application.Benefits.Queries.GetBenefitPlanById;

public sealed record GetBenefitPlanByIdQuery(Guid Id) : IRequest<BenefitPlanDto>;

public sealed class GetBenefitPlanByIdQueryHandler : IRequestHandler<GetBenefitPlanByIdQuery, BenefitPlanDto>
{
    private readonly IApplicationDbContext _db;

    public GetBenefitPlanByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<BenefitPlanDto> Handle(GetBenefitPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await _db.BenefitPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(BenefitPlan), request.Id);

        return new BenefitPlanDto(plan.Id, plan.Name, plan.Description, plan.Category, plan.MonthlyCost, plan.IsActive);
    }
}
