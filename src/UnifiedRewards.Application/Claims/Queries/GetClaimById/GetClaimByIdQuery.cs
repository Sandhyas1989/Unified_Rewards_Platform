using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Claims.Dtos;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Domain.Claims;

namespace UnifiedRewards.Application.Claims.Queries.GetClaimById;

public sealed record GetClaimByIdQuery(Guid Id) : IRequest<ClaimDto>;

public sealed class GetClaimByIdQueryHandler : IRequestHandler<GetClaimByIdQuery, ClaimDto>
{
    private readonly IApplicationDbContext _db;

    public GetClaimByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ClaimDto> Handle(GetClaimByIdQuery request, CancellationToken cancellationToken)
    {
        var claim = await _db.Claims
            .AsNoTracking()
            .Include(c => c.History)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.Id);

        return claim.ToDto();
    }
}
