using UnifiedRewards.Domain.Enums;
using UnifiedRewards.Domain.Promotions;

namespace UnifiedRewards.Application.Promotions.Dtos;

public sealed record PromotionNominationDto(
    Guid Id,
    Guid EmployeeId,
    Guid NominatedById,
    GradeBand CurrentGrade,
    GradeBand ProposedGrade,
    string Justification,
    PromotionStatus Status,
    Guid? ReviewerId,
    string? DecisionNotes,
    DateOnly? EffectiveDate,
    DateTime NominatedAtUtc,
    DateTime? DecisionAtUtc);

public static class PromotionMapping
{
    public static PromotionNominationDto ToDto(this PromotionNomination n) => new(
        n.Id, n.EmployeeId, n.NominatedById, n.CurrentGrade, n.ProposedGrade, n.Justification,
        n.Status, n.ReviewerId, n.DecisionNotes, n.EffectiveDate, n.NominatedAtUtc, n.DecisionAtUtc);
}
