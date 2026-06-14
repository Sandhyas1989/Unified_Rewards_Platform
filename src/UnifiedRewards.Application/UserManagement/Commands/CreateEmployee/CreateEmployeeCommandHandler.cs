using MediatR;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.UserManagement.Dtos;
using UnifiedRewards.Domain.UserManagement;

namespace UnifiedRewards.Application.UserManagement.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, UserDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public CreateEmployeeCommandHandler(IApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = new Employee
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Grade = request.Grade.Trim(),
            DateOfJoining = request.DateOfJoining,
            ManagerId = request.ManagerId
        };

        _db.Users.Add(employee);
        await _db.SaveChangesAsync(cancellationToken);

        return new UserDto(employee.Id, employee.FullName, employee.Email, employee.Role, employee.IsActive);
    }
}
