using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiedRewards.Application.UserManagement.Commands.Authenticate;
using UnifiedRewards.Application.UserManagement.Dtos;

namespace UnifiedRewards.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    /// <summary>Authenticates a user and returns a signed JWT bearer token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> Login(AuthenticateCommand command, CancellationToken cancellationToken)
        => Ok(await _sender.Send(command, cancellationToken));
}
