using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiedRewards.Application.Benefits.Commands.CancelEnrollment;
using UnifiedRewards.Application.Benefits.Commands.CreateBenefitPlan;
using UnifiedRewards.Application.Benefits.Commands.EnrollInBenefit;
using UnifiedRewards.Application.Benefits.Dtos;
using UnifiedRewards.Application.Benefits.Queries.GetBenefitPlanById;
using UnifiedRewards.Application.Benefits.Queries.GetBenefitPlans;
using UnifiedRewards.Application.Benefits.Queries.GetEnrollmentsByEmployee;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Api.Controllers;

[ApiController]
[Route("api/v1/benefits")]
[Authorize]
public sealed class BenefitsController : ApiControllerBase
{
    private readonly ISender _sender;

    public BenefitsController(ISender sender) => _sender = sender;

    // ---- Plans -----------------------------------------------------------

    /// <summary>Creates a benefit plan. HR Admin only.</summary>
    [HttpPost("plans")]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<BenefitPlanDto>> CreatePlan(CreateBenefitPlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPlanById), new { id = plan.Id }, plan);
    }

    /// <summary>Lists benefit plans, optionally filtered by category. Any authenticated user.</summary>
    [HttpGet("plans")]
    public async Task<ActionResult<IReadOnlyList<BenefitPlanDto>>> GetPlans(
        [FromQuery] BenefitCategory? category,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
        => Ok(await _sender.Send(new GetBenefitPlansQuery(category, activeOnly), cancellationToken));

    /// <summary>Gets a single benefit plan by id.</summary>
    [HttpGet("plans/{id:guid}")]
    public async Task<ActionResult<BenefitPlanDto>> GetPlanById(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetBenefitPlanByIdQuery(id), cancellationToken));

    // ---- Enrollments -----------------------------------------------------

    /// <summary>Enrols the current user in a benefit plan. Employees and Managers.</summary>
    [HttpPost("enrollments")]
    [Authorize(Roles = nameof(UserRole.Employee) + "," + nameof(UserRole.Manager))]
    public async Task<ActionResult<BenefitEnrollmentDto>> Enroll(EnrollRequest request, CancellationToken cancellationToken)
    {
        var command = new EnrollInBenefitCommand(CurrentUserId, request.BenefitPlanId, request.CoverageStartDate);
        var enrollment = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetMyEnrollments), null, enrollment);
    }

    /// <summary>Cancels one of the current user's enrolments. Employees and Managers.</summary>
    [HttpDelete("enrollments/{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Employee) + "," + nameof(UserRole.Manager))]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new CancelEnrollmentCommand(id, CurrentUserId), cancellationToken);
        return NoContent();
    }

    /// <summary>Lists the current user's own enrolments.</summary>
    [HttpGet("enrollments/me")]
    public async Task<ActionResult<IReadOnlyList<BenefitEnrollmentDto>>> GetMyEnrollments(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetEnrollmentsByEmployeeQuery(CurrentUserId), cancellationToken));

    /// <summary>Lists a given employee's enrolments. HR Admin / Finance only.</summary>
    [HttpGet("enrollments")]
    [Authorize(Roles = nameof(UserRole.HrAdmin) + "," + nameof(UserRole.Finance))]
    public async Task<ActionResult<IReadOnlyList<BenefitEnrollmentDto>>> GetByEmployee(
        [FromQuery] Guid employeeId,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetEnrollmentsByEmployeeQuery(employeeId), cancellationToken));

    /// <summary>Request body for enrolment; the employee id is taken from the token, not the body.</summary>
    public sealed record EnrollRequest(Guid BenefitPlanId, DateOnly CoverageStartDate);
}
