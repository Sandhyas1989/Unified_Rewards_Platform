using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiedRewards.Application.Compensation.Commands.ApproveCompensation;
using UnifiedRewards.Application.Compensation.Commands.GenerateCompensation;
using UnifiedRewards.Application.Compensation.Dtos;
using UnifiedRewards.Application.Compensation.Queries.GetCompensationByEmployee;
using UnifiedRewards.Application.Compensation.Queries.GetCompensationById;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Api.Controllers;

[ApiController]
[Route("api/v1/compensation")]
[Authorize]
public sealed class CompensationController : ApiControllerBase
{
    private readonly ISender _sender;

    public CompensationController(ISender sender) => _sender = sender;

    /// <summary>Generates a rule-based compensation structure for an employee. HR Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<CompensationStructureDto>> Generate(
        GenerateCompensationCommand command,
        CancellationToken cancellationToken)
    {
        var structure = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = structure.Id }, structure);
    }

    /// <summary>Approves a draft compensation structure. HR Admin / Finance.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = nameof(UserRole.HrAdmin) + "," + nameof(UserRole.Finance))]
    public async Task<ActionResult<CompensationStructureDto>> Approve(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new ApproveCompensationCommand(id), cancellationToken));

    /// <summary>Gets a compensation structure by id. HR Admin / Finance.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.HrAdmin) + "," + nameof(UserRole.Finance))]
    public async Task<ActionResult<CompensationStructureDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetCompensationByIdQuery(id), cancellationToken));

    /// <summary>Lists a given employee's compensation history. HR Admin / Finance.</summary>
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.HrAdmin) + "," + nameof(UserRole.Finance))]
    public async Task<ActionResult<IReadOnlyList<CompensationStructureDto>>> GetByEmployee(
        [FromQuery] Guid employeeId,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetCompensationByEmployeeQuery(employeeId), cancellationToken));

    /// <summary>Lists the current user's own compensation history.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<CompensationStructureDto>>> GetMine(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetCompensationByEmployeeQuery(CurrentUserId), cancellationToken));
}
