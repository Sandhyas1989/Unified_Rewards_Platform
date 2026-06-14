using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Compensation.Dtos;
using UnifiedRewards.Domain.Compensation;

namespace UnifiedRewards.Application.Compensation.Queries.GetCompensationById;

public sealed record GetCompensationByIdQuery(Guid Id) : IRequest<CompensationStructureDto>;

public sealed class GetCompensationByIdQueryHandler
    : IRequestHandler<GetCompensationByIdQuery, CompensationStructureDto>
{
    private readonly IApplicationDbContext _db;

    public GetCompensationByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CompensationStructureDto> Handle(GetCompensationByIdQuery request, CancellationToken cancellationToken)
    {
        var structure = await _db.CompensationStructures
            .AsNoTracking()
            .Include(s => s.Components)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CompensationStructure), request.Id);

        return structure.ToDto();
    }
}
