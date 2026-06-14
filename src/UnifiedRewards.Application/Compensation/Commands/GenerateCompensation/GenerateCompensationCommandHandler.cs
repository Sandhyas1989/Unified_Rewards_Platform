using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Compensation.Dtos;
using UnifiedRewards.Domain.Compensation;

namespace UnifiedRewards.Application.Compensation.Commands.GenerateCompensation;

public sealed class GenerateCompensationCommandHandler
    : IRequestHandler<GenerateCompensationCommand, CompensationStructureDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICompensationCalculator _calculator;

    public GenerateCompensationCommandHandler(IApplicationDbContext db, ICompensationCalculator calculator)
    {
        _db = db;
        _calculator = calculator;
    }

    public async Task<CompensationStructureDto> Handle(GenerateCompensationCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == request.EmployeeId, cancellationToken))
        {
            throw new NotFoundException("User", request.EmployeeId);
        }

        // Run the rules engine to expand the basic + grade into the full breakdown.
        var breakdown = _calculator.Calculate(request.AnnualBasic, request.Grade);

        var structure = new CompensationStructure
        {
            EmployeeId = request.EmployeeId,
            Grade = request.Grade,
            AnnualBasic = request.AnnualBasic,
            EffectiveFrom = request.EffectiveFrom,
            GrossAnnual = breakdown.GrossAnnual,
            TotalDeductions = breakdown.TotalDeductions,
            NetAnnual = breakdown.NetAnnual,
            Components = breakdown.Lines
                .Select(l => new CompensationComponent { Name = l.Name, Amount = l.Amount, Type = l.Type })
                .ToList()
        };

        _db.CompensationStructures.Add(structure);
        await _db.SaveChangesAsync(cancellationToken);

        return structure.ToDto();
    }
}
