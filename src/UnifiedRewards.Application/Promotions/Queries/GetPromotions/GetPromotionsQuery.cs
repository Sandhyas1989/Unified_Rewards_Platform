using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Common.Models;
using UnifiedRewards.Application.Promotions.Dtos;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Promotions.Queries.GetPromotions;

/// <summary>Lists nominations, optionally filtered by status, nominee, or nominator.</summary>
public sealed record GetPromotionsQuery(
    PromotionStatus? Status = null,
    Guid? EmployeeId = null,
    Guid? NominatedById = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<PromotionNominationDto>>;

public sealed class GetPromotionsQueryHandler : IRequestHandler<GetPromotionsQuery, PagedResult<PromotionNominationDto>>
{
    private readonly IApplicationDbContext _db;

    public GetPromotionsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<PromotionNominationDto>> Handle(GetPromotionsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.PromotionNominations.AsNoTracking();

        if (request.Status is not null)
        {
            query = query.Where(n => n.Status == request.Status);
        }

        if (request.EmployeeId is not null)
        {
            query = query.Where(n => n.EmployeeId == request.EmployeeId);
        }

        if (request.NominatedById is not null)
        {
            query = query.Where(n => n.NominatedById == request.NominatedById);
        }

        return await query
            .OrderByDescending(n => n.NominatedAtUtc)
            .Select(n => new PromotionNominationDto(
                n.Id, n.EmployeeId, n.NominatedById, n.CurrentGrade, n.ProposedGrade, n.Justification,
                n.Status, n.ReviewerId, n.DecisionNotes, n.EffectiveDate, n.NominatedAtUtc, n.DecisionAtUtc))
            .ToPagedResultAsync(new PageRequest(request.Page, request.PageSize), cancellationToken);
    }
}
