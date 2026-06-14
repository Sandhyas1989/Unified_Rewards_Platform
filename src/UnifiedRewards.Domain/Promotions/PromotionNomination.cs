using UnifiedRewards.Domain.Common;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.Promotions;

/// <summary>
/// A nomination to promote an employee from one grade band to a higher one. Aggregate root of
/// the Promotions module. Status changes go through guarded transition methods.
/// </summary>
public class PromotionNomination : BaseEntity
{
    public Guid EmployeeId { get; set; }

    /// <summary>The manager (or HR) who raised the nomination.</summary>
    public Guid NominatedById { get; set; }

    public GradeBand CurrentGrade { get; set; }

    public GradeBand ProposedGrade { get; set; }

    public string Justification { get; set; } = string.Empty;

    public PromotionStatus Status { get; private set; } = PromotionStatus.Nominated;

    public Guid? ReviewerId { get; private set; }

    public string? DecisionNotes { get; private set; }

    public DateOnly? EffectiveDate { get; private set; }

    public DateTime NominatedAtUtc { get; private set; } = DateTime.UtcNow;

    public DateTime? DecisionAtUtc { get; private set; }

    private static readonly IReadOnlyDictionary<PromotionStatus, PromotionStatus[]> AllowedTransitions =
        new Dictionary<PromotionStatus, PromotionStatus[]>
        {
            [PromotionStatus.Nominated] = new[] { PromotionStatus.UnderReview, PromotionStatus.Approved, PromotionStatus.Rejected },
            [PromotionStatus.UnderReview] = new[] { PromotionStatus.Approved, PromotionStatus.Rejected },
            [PromotionStatus.Approved] = Array.Empty<PromotionStatus>(),
            [PromotionStatus.Rejected] = Array.Empty<PromotionStatus>()
        };

    public static PromotionNomination Nominate(
        Guid employeeId, Guid nominatedById, GradeBand currentGrade, GradeBand proposedGrade, string justification)
        => new()
        {
            EmployeeId = employeeId,
            NominatedById = nominatedById,
            CurrentGrade = currentGrade,
            ProposedGrade = proposedGrade,
            Justification = justification.Trim(),
            Status = PromotionStatus.Nominated,
            NominatedAtUtc = DateTime.UtcNow
        };

    public void StartReview(Guid reviewerId) => TransitionTo(PromotionStatus.UnderReview, reviewerId, null);

    public void Approve(Guid reviewerId, string? notes, DateOnly effectiveDate)
    {
        TransitionTo(PromotionStatus.Approved, reviewerId, notes);
        EffectiveDate = effectiveDate;
    }

    public void Reject(Guid reviewerId, string? notes) => TransitionTo(PromotionStatus.Rejected, reviewerId, notes);

    private void TransitionTo(PromotionStatus target, Guid reviewerId, string? notes)
    {
        if (!AllowedTransitions[Status].Contains(target))
        {
            throw new InvalidPromotionTransitionException(Status, target);
        }

        Status = target;
        ReviewerId = reviewerId;
        DecisionNotes = notes;
        DecisionAtUtc = DateTime.UtcNow;
    }
}
