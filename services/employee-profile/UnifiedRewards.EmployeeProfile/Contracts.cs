using UnifiedRewards.EmployeeProfile.Domain;

namespace UnifiedRewards.EmployeeProfile;

public sealed record UserDto(Guid Id, string FullName, string Email, UserRole Role, bool IsActive, string? Grade);
public sealed record AuthResult(string Token, DateTime ExpiresAtUtc, UserDto User);
public sealed record LoginRequest(string Email, string Password);
public sealed record CreateEmployeeRequest(string FullName, string Email, string Password, string Grade, DateOnly DateOfJoining, Guid? ManagerId);

// Bonus campaign (Promotions) contracts
public sealed record CreatePromotionRequest(
    string Title, int CycleYear, string CycleQuarter,
    string FromGrade,          // optional eligible grade filter; empty string = all grades
    decimal BonusValue,        // monetary payout per approved nominee
    DateOnly NominationStart, DateOnly NominationEnd,
    int? MinTenureMonths, string? MinPerformanceRating, string? MinCurrentGrade);

public sealed record NominateRequest(Guid EmployeeId, string? Remarks);

public sealed record PromotionDecisionRequest(string? Remarks);

public sealed record PromotionDto(
    Guid Id, string Title, int CycleYear, string CycleQuarter,
    string FromGrade, decimal BonusValue,
    DateOnly NominationStart, DateOnly NominationEnd,
    int Status, int NominationCount, int ApprovedCount, DateTime CreatedAtUtc);

public sealed record NominationDto(
    Guid Id, Guid PromotionId, Guid EmployeeId, string? EmployeeName,
    Guid NominatedBy, DateOnly NominatedOn, int Outcome, string? Remarks, DateTime CreatedAtUtc);
