using MediatR;

namespace UnifiedRewards.Application.Benefits.Commands.CancelEnrollment;

/// <summary>
/// Cancels an active enrolment. <see cref="EmployeeId"/> comes from the authenticated
/// user's token and is used to enforce that callers can only cancel their own enrolments.
/// </summary>
public sealed record CancelEnrollmentCommand(Guid EnrollmentId, Guid EmployeeId) : IRequest;
