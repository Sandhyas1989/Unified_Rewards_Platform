using MediatR;
using UnifiedRewards.Application.Payroll.Dtos;

namespace UnifiedRewards.Application.Payroll.Commands.GeneratePayslip;

public sealed record GeneratePayslipCommand(Guid EmployeeId, int Year, int Month) : IRequest<PayslipDto>;
