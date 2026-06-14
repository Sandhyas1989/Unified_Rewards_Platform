using MediatR;
using UnifiedRewards.Application.UserManagement.Dtos;

namespace UnifiedRewards.Application.UserManagement.Commands.CreateEmployee;

public sealed record CreateEmployeeCommand(
    string FullName,
    string Email,
    string Password,
    string Grade,
    DateOnly DateOfJoining,
    Guid? ManagerId) : IRequest<UserDto>;
