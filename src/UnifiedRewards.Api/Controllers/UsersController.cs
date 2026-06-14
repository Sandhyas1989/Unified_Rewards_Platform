using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiedRewards.Application.UserManagement.Commands.CreateEmployee;
using UnifiedRewards.Application.UserManagement.Dtos;
using UnifiedRewards.Application.UserManagement.Queries.GetUserById;
using UnifiedRewards.Application.UserManagement.Queries.GetUsers;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender) => _sender = sender;

    /// <summary>Creates an employee. HR Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.HrAdmin))]
    public async Task<ActionResult<UserDto>> Create(CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var user = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>Gets a single user by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetUserByIdQuery(id), cancellationToken));

    /// <summary>Lists users (paged), optionally filtered by role. HR Admin / Finance / Manager.</summary>
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.HrAdmin) + "," + nameof(UserRole.Finance) + "," + nameof(UserRole.Manager))]
    public async Task<IActionResult> Get(
        [FromQuery] UserRole? role,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetUsersQuery(role, page, pageSize), cancellationToken));
}
