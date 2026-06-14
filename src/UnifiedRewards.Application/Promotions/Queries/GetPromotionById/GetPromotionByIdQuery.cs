using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Promotions.Dtos;
using UnifiedRewards.Domain.Promotions;

namespace UnifiedRewards.Application.Promotions.Queries.GetPromotionById;

public sealed record GetPromotionByIdQuery(Guid Id) : IRequest<PromotionNominationDto>;

public sealed class GetPromotionByIdQueryHandler : IRequestHandler<GetPromotionByIdQuery, PromotionNominationDto>
{
    private readonly IApplicationDbContext _db;

    public GetPromotionByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PromotionNominationDto> Handle(GetPromotionByIdQuery request, CancellationToken cancellationToken)
    {
        var nomination = await _db.PromotionNominations
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(PromotionNomination), request.Id);

        return nomination.ToDto();
    }
}
