using MediatR;
using UnifiedRewards.Application.Benefits.Dtos;

namespace UnifiedRewards.Application.Benefits.Commands.EnrollInBenefit;

/// <summary>
/// Enrols an employee in a benefit plan. <see cref="EmployeeId"/> is supplied by the
/// API from the authenticated user's token, not from the request body.
/// </summary>
public sealed record EnrollInBenefitCommand(
    Guid EmployeeId,
    Guid BenefitPlanId,
    DateOnly CoverageStartDate) : IRequest<BenefitEnrollmentDto>;
