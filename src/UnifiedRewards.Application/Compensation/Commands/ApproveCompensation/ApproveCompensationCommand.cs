using MediatR;
using UnifiedRewards.Application.Compensation.Dtos;

namespace UnifiedRewards.Application.Compensation.Commands.ApproveCompensation;

public sealed record ApproveCompensationCommand(Guid Id) : IRequest<CompensationStructureDto>;
