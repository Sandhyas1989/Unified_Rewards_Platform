using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Common.Models;
using UnifiedRewards.Application.UserManagement.Dtos;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.UserManagement.Queries.GetUsers;

public sealed record GetUsersQuery(UserRole? Role = null, int? Page = null, int? PageSize = null)
    : IRequest<PagedResult<UserDto>>;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IApplicationDbContext _db;

    public GetUsersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Users.AsNoTracking();

        if (request.Role is not null)
        {
            query = query.Where(u => u.Role == request.Role);
        }

        return await query
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto(u.Id, u.FullName, u.Email, u.Role, u.IsActive))
            .ToPagedResultAsync(new PageRequest(request.Page, request.PageSize), cancellationToken);
    }
}
