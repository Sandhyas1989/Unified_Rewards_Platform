namespace UnifiedRewards.ReportingCompliance;

public sealed record StatusAmountRow(string Status, int Count, decimal TotalAmount);
public sealed record DashboardDto(
    DateTime GeneratedAtUtc,
    IReadOnlyList<StatusAmountRow> ClaimsByStatus,
    IReadOnlyList<StatusAmountRow> SettlementsByStatus,
    int TotalClaims,
    int TotalSettlements,
    string Note);
