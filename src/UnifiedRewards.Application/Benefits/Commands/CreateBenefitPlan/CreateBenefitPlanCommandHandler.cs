using MediatR;
using UnifiedRewards.Application.Benefits.Dtos;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Domain.Benefits;

namespace UnifiedRewards.Application.Benefits.Commands.CreateBenefitPlan;

public sealed class CreateBenefitPlanCommandHandler : IRequestHandler<CreateBenefitPlanCommand, BenefitPlanDto>
{
    private readonly IApplicationDbContext _db;

    public CreateBenefitPlanCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<BenefitPlanDto> Handle(CreateBenefitPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = new BenefitPlan
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category,
            MonthlyCost = request.MonthlyCost
        };

        _db.BenefitPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);

        return new BenefitPlanDto(plan.Id, plan.Name, plan.Description, plan.Category, plan.MonthlyCost, plan.IsActive);
    }
}
