using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiedRewards.Application.Promotions.Commands.ApprovePromotion;
using UnifiedRewards.Application.Promotions.Commands.NominateForPromotion;
using UnifiedRewards.Application.Promotions.Commands.RejectPromotion;
using UnifiedRewards.Application.Promotions.Commands.StartPromotionReview;
using UnifiedRewards.Application.Promotions.Dtos;
using UnifiedRewards.Application.Promotions.Queries.GetPromotionById;
using UnifiedRewards.Application.Promotions.Queries.GetPromotions;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Api.Controllers;

[ApiController]
[Route("api/v1/promotions")]
[Authorize]
public sealed class PromotionsController : ApiControllerBase
{
    private readonly ISender _sender;

    public PromotionsController(ISender sender) => _sender = sender;

    /// <summary>Raises a promotion nomination for an employee. Manager / HR Admin.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Manager) + "," + nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<PromotionNominationDto>> Nominate(NominateRequest request, CancellationToken cancellationToken)
    {
        var command = new NominateForPromotionCommand(
            request.EmployeeId, CurrentUserId, request.CurrentGrade, request.ProposedGrade, request.Justification);
        var nomination = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = nomination.Id }, nomination);
    }

    /// <summary>Moves a nomination into committee review. HR Admin.</summary>
    [HttpPost("{id:guid}/start-review")]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<PromotionNominationDto>> StartReview(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new StartPromotionReviewCommand(id, CurrentUserId), cancellationToken));

    /// <summary>Approves a nomination (emits a PromotionApproved event). HR Admin.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<PromotionNominationDto>> Approve(Guid id, PromotionDecision body, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new ApprovePromotionCommand(id, CurrentUserId, body.EffectiveDate, body.Notes), cancellationToken));

    /// <summary>Rejects a nomination. HR Admin.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<PromotionNominationDto>> Reject(Guid id, [FromBody] PromotionRejection? body, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new RejectPromotionCommand(id, CurrentUserId, body?.Notes), cancellationToken));

    /// <summary>Gets a nomination by id. Manager / HR Admin.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Manager) + "," + nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<PromotionNominationDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetPromotionByIdQuery(id), cancellationToken));

    /// <summary>Lists nominations (paged), optionally filtered. HR Admin.</summary>
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<IActionResult> Get(
        [FromQuery] PromotionStatus? status,
        [FromQuery] Guid? employeeId,
        [FromQuery] Guid? nominatedById,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetPromotionsQuery(status, employeeId, nominatedById, page, pageSize), cancellationToken));

    /// <summary>Lists the current user's own nominations (paged, as nominee).</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetPromotionsQuery(EmployeeId: CurrentUserId, Page: page, PageSize: pageSize), cancellationToken));

    public sealed record NominateRequest(Guid EmployeeId, GradeBand CurrentGrade, GradeBand ProposedGrade, string Justification);

    public sealed record PromotionDecision(DateOnly EffectiveDate, string? Notes);

    public sealed record PromotionRejection(string? Notes);
}
