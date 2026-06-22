using System.ComponentModel.DataAnnotations;

namespace UnifiedRewards.ReimbursementWorkflow.Domain;

public enum ClaimType { Travel = 0, Medical = 1, Food = 2, Internet = 3, Training = 4, Other = 5 }
public enum ClaimStatus { Submitted = 0, UnderReview = 1, Approved = 2, Rejected = 3, Settled = 4 }

public sealed class InvalidClaimTransitionException : Exception
{
    public InvalidClaimTransitionException(ClaimStatus from, ClaimStatus to)
        : base($"Cannot transition a claim from '{from}' to '{to}'.") { }
}

// Reimbursement claim with a guarded state machine (ported from the monolith's Claims module).
public class Claim
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; set; }
    public ClaimType Type { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public string Description { get; set; } = string.Empty;
    public ClaimStatus Status { get; private set; } = ClaimStatus.Submitted;
    public Guid? ReviewerId { get; private set; }
    public string? DecisionNotes { get; private set; }
    public string? SettlementReference { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? DecisionAtUtc { get; private set; }
    public DateTime? SettledAtUtc { get; private set; }
    public ICollection<ClaimTransition> History { get; set; } = new List<ClaimTransition>();

    // Optimistic-concurrency token. Configured as a store-generated rowversion on SQL Server only
    // (see ReimbursementDbContext); on SQLite/local it stays a plain column so inserts don't hit a
    // NOT NULL/store-generation mismatch.
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    private static readonly IReadOnlyDictionary<ClaimStatus, ClaimStatus[]> Allowed =
        new Dictionary<ClaimStatus, ClaimStatus[]>
        {
            [ClaimStatus.Submitted] = new[] { ClaimStatus.UnderReview, ClaimStatus.Approved, ClaimStatus.Rejected },
            [ClaimStatus.UnderReview] = new[] { ClaimStatus.Approved, ClaimStatus.Rejected },
            [ClaimStatus.Approved] = new[] { ClaimStatus.Settled },
            [ClaimStatus.Rejected] = Array.Empty<ClaimStatus>(),
            [ClaimStatus.Settled] = Array.Empty<ClaimStatus>(),
        };

    public static Claim Submit(Guid tenantId, Guid employeeId, ClaimType type, decimal amount, string description, string currencyCode = "INR")
    {
        var c = new Claim { TenantId = tenantId, EmployeeId = employeeId, Type = type, Amount = amount, CurrencyCode = currencyCode, Description = description.Trim() };
        c.History.Add(new ClaimTransition { FromStatus = null, ToStatus = ClaimStatus.Submitted, ActorId = employeeId });
        return c;
    }

    public void StartReview(Guid reviewerId) => Transition(ClaimStatus.UnderReview, reviewerId, null);
    public void Approve(Guid reviewerId, string? notes) { Transition(ClaimStatus.Approved, reviewerId, notes); ReviewerId = reviewerId; DecisionNotes = notes; DecisionAtUtc = DateTime.UtcNow; }
    public void Reject(Guid reviewerId, string? notes) { Transition(ClaimStatus.Rejected, reviewerId, notes); ReviewerId = reviewerId; DecisionNotes = notes; DecisionAtUtc = DateTime.UtcNow; }
    public void Settle(Guid actorId, string settlementReference) { Transition(ClaimStatus.Settled, actorId, null); SettlementReference = settlementReference; SettledAtUtc = DateTime.UtcNow; }

    private void Transition(ClaimStatus target, Guid actorId, string? notes)
    {
        if (!Allowed[Status].Contains(target)) throw new InvalidClaimTransitionException(Status, target);
        var from = Status;
        Status = target;
        History.Add(new ClaimTransition { FromStatus = from, ToStatus = target, ActorId = actorId, Notes = notes });
    }
}

public class ClaimTransition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClaimId { get; set; }
    public ClaimStatus? FromStatus { get; set; }
    public ClaimStatus ToStatus { get; set; }
    public Guid ActorId { get; set; }
    public string? Notes { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
