namespace UnifiedRewards.Messaging.Events;

// Versioned integration-event contracts shared across services (the seed of the Step-5 shared-contracts
// package). Additive-only; the type name is the EventType on the wire.

// --- Claim lifecycle (published by Reimbursement Workflow) ---
public sealed record ClaimSubmitted(
    Guid ClaimId, Guid EmployeeId, int Type, decimal Amount, DateTime OccurredAtUtc);

public sealed record ClaimApproved(
    Guid ClaimId, Guid EmployeeId, Guid ReviewerId, decimal Amount, DateTime OccurredAtUtc);

public sealed record ClaimRejected(
    Guid ClaimId, Guid EmployeeId, Guid ReviewerId, string? Reason, DateTime OccurredAtUtc);

public sealed record ClaimSettled(
    Guid ClaimId, Guid EmployeeId, string SettlementReference, DateTime OccurredAtUtc);

// --- Document leg (Document & Receipt Processing → Reimbursement advances claim to "In Review") ---
public sealed record DocumentProcessed(
    Guid ClaimId, Guid DocumentId, decimal? ExtractedAmount, decimal? OcrConfidence, DateTime OccurredAtUtc);

// --- Settlement leg (Reimbursement ⇄ Payroll Integration saga) ---
public sealed record SettlementRequested(
    Guid ClaimId, Guid EmployeeId, decimal Amount, DateTime OccurredAtUtc);

public sealed record SettlementCompleted(
    Guid ClaimId, Guid SettlementId, bool Success, string Reference, string? Error, DateTime OccurredAtUtc);

// --- Bonus campaign leg (Employee Profile → Payroll) ---
// Published when HR Admin approves a nomination in a bonus campaign cycle.
public sealed record BonusAwarded(
    Guid EmployeeId, Guid CampaignId, Guid NominationId, decimal Amount, DateTime OccurredAtUtc);
