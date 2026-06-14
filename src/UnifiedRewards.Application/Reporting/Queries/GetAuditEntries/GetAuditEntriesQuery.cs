using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Common.Models;
using UnifiedRewards.Application.Reporting.Dtos;

namespace UnifiedRewards.Application.Reporting.Queries.GetAuditEntries;

public sealed record GetAuditEntriesQuery(
    Guid? UserId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? Action = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<AuditEntryDto>>;

public sealed class GetAuditEntriesQueryHandler : IRequestHandler<GetAuditEntriesQuery, PagedResult<AuditEntryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAuditEntriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<AuditEntryDto>> Handle(GetAuditEntriesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AuditEntries.AsNoTracking();

        if (request.UserId is not null)
        {
            query = query.Where(a => a.UserId == request.UserId);
        }

        if (request.FromUtc is not null)
        {
            query = query.Where(a => a.OccurredAtUtc >= request.FromUtc);
        }

        if (request.ToUtc is not null)
        {
            query = query.Where(a => a.OccurredAtUtc <= request.ToUtc);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(a => a.Action == request.Action);
        }

        return await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Select(a => new AuditEntryDto(a.Id, a.UserId, a.UserEmail, a.Action, a.Succeeded, a.Error, a.OccurredAtUtc))
            .ToPagedResultAsync(new PageRequest(request.Page, request.PageSize), cancellationToken);
    }
}
