using MediatR;
using UnifiedRewards.Application.Compensation.Dtos;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Compensation.Commands.GenerateCompensation;

public sealed record GenerateCompensationCommand(
    Guid EmployeeId,
    GradeBand Grade,
    decimal AnnualBasic,
    DateOnly EffectiveFrom) : IRequest<CompensationStructureDto>;
