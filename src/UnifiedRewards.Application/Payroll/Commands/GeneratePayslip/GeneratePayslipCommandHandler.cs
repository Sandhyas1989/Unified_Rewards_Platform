using MediatR;
using Microsoft.EntityFrameworkCore;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Payroll.Dtos;
using UnifiedRewards.Domain.Enums;
using UnifiedRewards.Domain.Payroll;

namespace UnifiedRewards.Application.Payroll.Commands.GeneratePayslip;

public sealed class GeneratePayslipCommandHandler : IRequestHandler<GeneratePayslipCommand, PayslipDto>
{
    private readonly IApplicationDbContext _db;

    public GeneratePayslipCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<PayslipDto> Handle(GeneratePayslipCommand request, CancellationToken cancellationToken)
    {
        // Idempotent: return the existing payslip for this period if already generated.
        var existing = await _db.Payslips
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.EmployeeId == request.EmployeeId && p.Year == request.Year && p.Month == request.Month,
                cancellationToken);

        if (existing is not null)
        {
            return existing.ToDto();
        }

        // Derive the monthly figures from the employee's latest approved compensation structure.
        var compensation = await _db.CompensationStructures
            .AsNoTracking()
            .Where(c => c.EmployeeId == request.EmployeeId && c.Status == CompensationStatus.Approved)
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Approved CompensationStructure for employee", request.EmployeeId);

        var payslip = new Payslip
        {
            EmployeeId = request.EmployeeId,
            Year = request.Year,
            Month = request.Month,
            GrossMonthly = Math.Round(compensation.GrossAnnual / 12m, 2),
            TotalDeductionsMonthly = Math.Round(compensation.TotalDeductions / 12m, 2),
            NetMonthly = Math.Round(compensation.NetAnnual / 12m, 2),
            CompensationStructureId = compensation.Id
        };

        _db.Payslips.Add(payslip);
        await _db.SaveChangesAsync(cancellationToken);

        return payslip.ToDto();
    }
}
