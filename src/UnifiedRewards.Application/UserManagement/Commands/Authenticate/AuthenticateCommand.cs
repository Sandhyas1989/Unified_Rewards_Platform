using MediatR;
using UnifiedRewards.Application.UserManagement.Dtos;

namespace UnifiedRewards.Application.UserManagement.Commands.Authenticate;

public sealed record AuthenticateCommand(string Email, string Password) : IRequest<AuthResult>;
