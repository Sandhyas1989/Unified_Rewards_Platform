using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Compensation.Dtos;
using UnifiedRewards.Domain.Compensation;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Application.Compensation.Commands.ApproveCompensation;

public sealed class ApproveCompensationCommandHandler
    : IRequestHandler<ApproveCompensationCommand, CompensationStructureDto>
{
    private readonly IApplicationDbContext _db;

    public ApproveCompensationCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<CompensationStructureDto> Handle(ApproveCompensationCommand request, CancellationToken cancellationToken)
    {
        var structure = await _db.CompensationStructures
            .Include(s => s.Components)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CompensationStructure), request.Id);

        if (structure.Status != CompensationStatus.Approved)
        {
            structure.Status = CompensationStatus.Approved;
            structure.ApprovedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return structure.ToDto();
    }
}
