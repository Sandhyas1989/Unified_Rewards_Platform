using MediatR;
using UnifiedRewards.Application.Promotions.Dtos;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Promotions.Commands.NominateForPromotion;

/// <summary><see cref="NominatedById"/> is supplied from the authenticated user, not the body.</summary>
public sealed record NominateForPromotionCommand(
    Guid EmployeeId,
    Guid NominatedById,
    GradeBand CurrentGrade,
    GradeBand ProposedGrade,
    string Justification) : IRequest<PromotionNominationDto>;
