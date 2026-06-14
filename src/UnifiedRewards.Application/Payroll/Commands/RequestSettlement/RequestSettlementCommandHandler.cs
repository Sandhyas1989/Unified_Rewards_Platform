using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Payroll.Dtos;
using UnifiedRewards.Domain.Payroll;

namespace UnifiedRewards.Application.Payroll.Commands.RequestSettlement;

public sealed class RequestSettlementCommandHandler : IRequestHandler<RequestSettlementCommand, SettlementRequestDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISettlementQueue _queue;

    public RequestSettlementCommandHandler(IApplicationDbContext db, ISettlementQueue queue)
    {
        _db = db;
        _queue = queue;
    }

    public async Task<SettlementRequestDto> Handle(RequestSettlementCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == request.EmployeeId, cancellationToken))
        {
            throw new NotFoundException("User", request.EmployeeId);
        }

        var settlement = new SettlementRequest
        {
            EmployeeId = request.EmployeeId,
            Amount = request.Amount
        };
        settlement.Reference = $"SET-{settlement.Id:N}";

        _db.SettlementRequests.Add(settlement);
        await _db.SaveChangesAsync(cancellationToken);

        // Hand off for asynchronous processing once the row is committed.
        await _queue.EnqueueAsync(settlement.Id, cancellationToken);

        return settlement.ToDto();
    }
}
