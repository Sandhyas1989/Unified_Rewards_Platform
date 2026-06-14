namespace UnifiedRewards.Application.Reporting.Dtos;

public sealed record StatusAmountRow(string Status, int Count, decimal TotalAmount);

public sealed record CountRow(string Key, int Count);

/// <summary>Cross-module operational summary built from LINQ aggregations.</summary>
public sealed record DashboardReportDto(
    DateTime GeneratedAtUtc,
    IReadOnlyList<StatusAmountRow> ClaimsByStatus,
    IReadOnlyList<StatusAmountRow> SettlementsByStatus,
    IReadOnlyList<CountRow> HeadcountByRole,
    IReadOnlyList<CountRow> ActiveEnrollmentsByPlan,
    int ApprovedPromotions,
    int TotalPayslips);

public sealed record ClaimReportRow(
    Guid ClaimId,
    string Type,
    decimal Amount,
    string Status,
    DateTime SubmittedAtUtc,
    DateTime? SettledAtUtc);

/// <summary>A generated spreadsheet returned to the API for download.</summary>
public sealed record ExcelFile(byte[] Content, string FileName);

public sealed record AuditEntryDto(
    Guid Id,
    Guid? UserId,
    string? UserEmail,
    string Action,
    bool Succeeded,
    string? Error,
    DateTime OccurredAtUtc);
