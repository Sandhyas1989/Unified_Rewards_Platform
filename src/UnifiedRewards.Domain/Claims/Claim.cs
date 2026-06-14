using UnifiedRewards.Domain.Common;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.Claims;

/// <summary>
/// A reimbursement claim. Aggregate root of the Claims &amp; Documents module.
/// Status changes go through the guarded transition methods, which enforce the
/// allowed state machine and append an audit entry to <see cref="History"/>.
/// </summary>
public class Claim : BaseEntity
{
    public Guid EmployeeId { get; set; }

    public ClaimType Type { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public ClaimStatus Status { get; private set; } = ClaimStatus.Submitted;

    // ---- Attached receipt document (via IFileStorage) + OCR results (via IOcrEngine) ----
    public string? ReceiptReference { get; set; }
    public string? ReceiptFileName { get; set; }
    public string? ReceiptContentType { get; set; }
    public string? OcrText { get; set; }
    public decimal? OcrConfidence { get; set; }
    public decimal? OcrExtractedAmount { get; set; }

    // ---- Workflow outcome ----
    public Guid? ReviewerId { get; private set; }
    public string? DecisionNotes { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? DecisionAtUtc { get; private set; }
    public DateTime? SettledAtUtc { get; private set; }
    public string? PayrollReference { get; private set; }

    public ICollection<ClaimTransition> History { get; set; } = new List<ClaimTransition>();

    /// <summary>Allowed target states keyed by current state — the state machine's transition table.</summary>
    private static readonly IReadOnlyDictionary<ClaimStatus, ClaimStatus[]> AllowedTransitions =
        new Dictionary<ClaimStatus, ClaimStatus[]>
        {
            [ClaimStatus.Submitted] = new[] { ClaimStatus.UnderReview, ClaimStatus.Approved, ClaimStatus.Rejected },
            [ClaimStatus.UnderReview] = new[] { ClaimStatus.Approved, ClaimStatus.Rejected },
            [ClaimStatus.Approved] = new[] { ClaimStatus.Settled },
            [ClaimStatus.Rejected] = Array.Empty<ClaimStatus>(),
            [ClaimStatus.Settled] = Array.Empty<ClaimStatus>()
        };

    /// <summary>Factory for a newly submitted claim, seeding the initial history entry.</summary>
    public static Claim Submit(Guid employeeId, ClaimType type, decimal amount, string description)
    {
        var claim = new Claim
        {
            EmployeeId = employeeId,
            Type = type,
            Amount = amount,
            Description = description.Trim(),
            Status = ClaimStatus.Submitted,
            SubmittedAtUtc = DateTime.UtcNow
        };
        claim.History.Add(new ClaimTransition
        {
            FromStatus = null,
            ToStatus = ClaimStatus.Submitted,
            ActorId = employeeId
        });
        return claim;
    }

    public void StartReview(Guid reviewerId) => TransitionTo(ClaimStatus.UnderReview, reviewerId, null);

    public void Approve(Guid reviewerId, string? notes)
    {
        TransitionTo(ClaimStatus.Approved, reviewerId, notes);
        ReviewerId = reviewerId;
        DecisionNotes = notes;
        DecisionAtUtc = DateTime.UtcNow;
    }

    public void Reject(Guid reviewerId, string? notes)
    {
        TransitionTo(ClaimStatus.Rejected, reviewerId, notes);
        ReviewerId = reviewerId;
        DecisionNotes = notes;
        DecisionAtUtc = DateTime.UtcNow;
    }

    public void Settle(Guid actorId, string payrollReference)
    {
        TransitionTo(ClaimStatus.Settled, actorId, null);
        PayrollReference = payrollReference;
        SettledAtUtc = DateTime.UtcNow;
    }

    private void TransitionTo(ClaimStatus target, Guid actorId, string? notes)
    {
        if (!AllowedTransitions[Status].Contains(target))
        {
            throw new InvalidClaimTransitionException(Status, target);
        }

        var from = Status;
        Status = target;
        History.Add(new ClaimTransition
        {
            FromStatus = from,
            ToStatus = target,
            ActorId = actorId,
            Notes = notes
        });
    }
}
