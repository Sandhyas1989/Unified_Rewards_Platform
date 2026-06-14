using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Claims.Dtos;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Common.Interfaces.Integration;
using UnifiedRewards.Domain.Claims;

namespace UnifiedRewards.Application.Claims.Commands.SettleClaim;

public sealed record SettleClaimCommand(Guid ClaimId, Guid ActorId) : IRequest<ClaimDto>;

public sealed class SettleClaimCommandHandler : IRequestHandler<SettleClaimCommand, ClaimDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPayrollService _payrollService;

    public SettleClaimCommandHandler(IApplicationDbContext db, IPayrollService payrollService)
    {
        _db = db;
        _payrollService = payrollService;
    }

    public async Task<ClaimDto> Handle(SettleClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _db.Claims
            .Include(c => c.History)
            .FirstOrDefaultAsync(c => c.Id == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        // Push the reimbursement to payroll (integration seam) before recording settlement.
        var reference = $"CLAIM-{claim.Id:N}";
        var pushed = await _payrollService.PushSettlementAsync(
            claim.EmployeeId, claim.Amount, reference, cancellationToken);

        if (!pushed)
        {
            throw new InvalidOperationException("Payroll settlement was not accepted.");
        }

        claim.Settle(request.ActorId, reference);
        await _db.SaveChangesAsync(cancellationToken);

        return claim.ToDto();
    }
}
