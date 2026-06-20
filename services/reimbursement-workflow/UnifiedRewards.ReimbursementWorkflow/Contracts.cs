namespace UnifiedRewards.ReimbursementWorkflow;

public sealed record ClaimTransitionDto(int? FromStatus, int ToStatus, Guid ActorId, string? Notes, DateTime OccurredAtUtc);
public sealed record ClaimDto(
    Guid Id, Guid EmployeeId, int Type, decimal Amount, string CurrencyCode, string Description, int Status,
    Guid? ReviewerId, string? DecisionNotes, string? SettlementReference,
    DateTime SubmittedAtUtc, DateTime? DecisionAtUtc, DateTime? SettledAtUtc, IReadOnlyList<ClaimTransitionDto> History);
public sealed record SubmitClaimRequest(int Type, decimal Amount, string Description, string CurrencyCode = "INR");
public sealed record DecisionRequest(string? Notes);
