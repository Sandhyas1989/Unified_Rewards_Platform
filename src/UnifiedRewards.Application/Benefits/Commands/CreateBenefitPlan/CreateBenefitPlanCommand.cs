using MediatR;
using UnifiedRewards.Application.Benefits.Dtos;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Benefits.Commands.CreateBenefitPlan;

public sealed record CreateBenefitPlanCommand(
    string Name,
    string Description,
    BenefitCategory Category,
    decimal MonthlyCost) : IRequest<BenefitPlanDto>;
