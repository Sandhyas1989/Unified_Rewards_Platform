using MediatR;
using UnifiedRewards.Application.Payroll.Dtos;

namespace UnifiedRewards.Application.Payroll.Commands.RequestSettlement;

public sealed record RequestSettlementCommand(Guid EmployeeId, decimal Amount) : IRequest<SettlementRequestDto>;
