using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiedRewards.Application.Claims.Commands.ApproveClaim;
using UnifiedRewards.Application.Claims.Commands.RejectClaim;
using UnifiedRewards.Application.Claims.Commands.SettleClaim;
using UnifiedRewards.Application.Claims.Commands.StartClaimReview;
using UnifiedRewards.Application.Claims.Commands.SubmitClaim;
using UnifiedRewards.Application.Claims.Dtos;
using UnifiedRewards.Application.Claims.Queries.DownloadReceipt;
using UnifiedRewards.Application.Claims.Queries.GetClaimById;
using UnifiedRewards.Application.Claims.Queries.GetClaims;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Api.Controllers;

[ApiController]
[Route("api/v1/claims")]
[Authorize]
public sealed class ClaimsController : ApiControllerBase
{
    private const string Reviewers = nameof(UserRole.Manager) + "," + nameof(UserRole.Finance) + "," + nameof(UserRole.HrAdmin);

    private readonly ISender _sender;

    public ClaimsController(ISender sender) => _sender = sender;

    /// <summary>Submits a claim for the current user, with an optional receipt to store and OCR-scan.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Employee) + "," + nameof(UserRole.Manager))]
    public async Task<ActionResult<ClaimDto>> Submit([FromForm] SubmitClaimForm form, CancellationToken cancellationToken)
    {
        byte[]? receipt = null;
        if (form.Receipt is { Length: > 0 })
        {
            using var ms = new MemoryStream();
            await form.Receipt.CopyToAsync(ms, cancellationToken);
            receipt = ms.ToArray();
        }

        var command = new SubmitClaimCommand(
            CurrentUserId, form.Type, form.Amount, form.Description ?? string.Empty,
            receipt, form.Receipt?.FileName, form.Receipt?.ContentType);

        var claim = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = claim.Id }, claim);
    }

    /// <summary>Moves a submitted claim into review. Reviewers only.</summary>
    [HttpPost("{id:guid}/start-review")]
    [Authorize(Roles = Reviewers)]
    public async Task<ActionResult<ClaimDto>> StartReview(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new StartClaimReviewCommand(id, CurrentUserId), cancellationToken));

    /// <summary>Approves a claim. Manager / Finance / HR Admin.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Reviewers)]
    public async Task<ActionResult<ClaimDto>> Approve(Guid id, [FromBody] ClaimDecision? body, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new ApproveClaimCommand(id, CurrentUserId, body?.Notes), cancellationToken));

    /// <summary>Rejects a claim. Manager / Finance / HR Admin.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Reviewers)]
    public async Task<ActionResult<ClaimDto>> Reject(Guid id, [FromBody] ClaimDecision? body, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new RejectClaimCommand(id, CurrentUserId, body?.Notes), cancellationToken));

    /// <summary>Settles an approved claim, pushing the reimbursement to payroll. Finance only.</summary>
    [HttpPost("{id:guid}/settle")]
    [Authorize(Roles = nameof(UserRole.Finance))]
    public async Task<ActionResult<ClaimDto>> Settle(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new SettleClaimCommand(id, CurrentUserId), cancellationToken));

    /// <summary>Gets a claim with its full transition history. Reviewers only.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = Reviewers)]
    public async Task<ActionResult<ClaimDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetClaimByIdQuery(id), cancellationToken));

    /// <summary>Lists claims (paged) for review, optionally filtered by status/employee. Reviewers only.</summary>
    [HttpGet]
    [Authorize(Roles = Reviewers)]
    public async Task<IActionResult> Get(
        [FromQuery] ClaimStatus? status,
        [FromQuery] Guid? employeeId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetClaimsQuery(status, employeeId, page, pageSize), cancellationToken));

    /// <summary>Lists the current user's own claims (paged).</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMine(
        [FromQuery] ClaimStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetClaimsQuery(status, CurrentUserId, page, pageSize), cancellationToken));

    /// <summary>Downloads a claim's receipt. Reviewers, or the owning employee.</summary>
    [HttpGet("{id:guid}/receipt")]
    public async Task<IActionResult> Receipt(Guid id, CancellationToken cancellationToken)
    {
        var isReviewer = User.IsInRole(nameof(UserRole.Manager))
                         || User.IsInRole(nameof(UserRole.Finance))
                         || User.IsInRole(nameof(UserRole.HrAdmin));

        var file = await _sender.Send(new DownloadReceiptQuery(id, CurrentUserId, isReviewer), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Multipart form for claim submission (the receipt file is optional).</summary>
    public sealed class SubmitClaimForm
    {
        public ClaimType Type { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public IFormFile? Receipt { get; set; }
    }

    /// <summary>Optional reviewer notes for approve/reject.</summary>
    public sealed record ClaimDecision(string? Notes);
}
