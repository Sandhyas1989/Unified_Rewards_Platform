using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.UserManagement.Dtos;

namespace UnifiedRewards.Application.UserManagement.Commands.Authenticate;

public sealed class AuthenticateCommandHandler : IRequestHandler<AuthenticateCommand, AuthResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthenticateCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResult> Handle(AuthenticateCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user);

        return new AuthResult(
            token,
            expiresAtUtc,
            new UserDto(user.Id, user.FullName, user.Email, user.Role, user.IsActive));
    }
}
