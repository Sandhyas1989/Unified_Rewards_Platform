using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Claims.Dtos;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Common.Models;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Claims.Queries.GetClaims;

/// <summary>Lists claims, optionally filtered by status and/or employee. Drives both the
/// reviewer queue and an employee's "my claims" view.</summary>
public sealed record GetClaimsQuery(
    ClaimStatus? Status = null, Guid? EmployeeId = null, int? Page = null, int? PageSize = null)
    : IRequest<PagedResult<ClaimDto>>;

public sealed class GetClaimsQueryHandler : IRequestHandler<GetClaimsQuery, PagedResult<ClaimDto>>
{
    private readonly IApplicationDbContext _db;

    public GetClaimsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ClaimDto>> Handle(GetClaimsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Claims.AsNoTracking().Include(c => c.History).AsQueryable();

        if (request.Status is not null)
        {
            query = query.Where(c => c.Status == request.Status);
        }

        if (request.EmployeeId is not null)
        {
            query = query.Where(c => c.EmployeeId == request.EmployeeId);
        }

        var page = new PageRequest(request.Page, request.PageSize);
        var total = await query.CountAsync(cancellationToken);
        var claims = await query
            .OrderByDescending(c => c.SubmittedAtUtc)
            .Skip(page.Skip).Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ClaimDto>(claims.Select(c => c.ToDto()).ToList(), page.Page, page.PageSize, total);
    }
}
