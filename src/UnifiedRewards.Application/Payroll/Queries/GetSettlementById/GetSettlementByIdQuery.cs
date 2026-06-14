using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Payroll.Dtos;
using UnifiedRewards.Domain.Payroll;

namespace UnifiedRewards.Application.Payroll.Queries.GetSettlementById;

public sealed record GetSettlementByIdQuery(Guid Id) : IRequest<SettlementRequestDto>;

public sealed class GetSettlementByIdQueryHandler : IRequestHandler<GetSettlementByIdQuery, SettlementRequestDto>
{
    private readonly IApplicationDbContext _db;

    public GetSettlementByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<SettlementRequestDto> Handle(GetSettlementByIdQuery request, CancellationToken cancellationToken)
    {
        var settlement = await _db.SettlementRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(SettlementRequest), request.Id);

        return settlement.ToDto();
    }
}
