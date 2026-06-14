using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiedRewards.Application.Payroll.Commands.GeneratePayslip;
using UnifiedRewards.Application.Payroll.Commands.RequestSettlement;
using UnifiedRewards.Application.Payroll.Dtos;
using UnifiedRewards.Application.Payroll.Queries.GetPayslipsByEmployee;
using UnifiedRewards.Application.Payroll.Queries.GetSettlementById;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Api.Controllers;

[ApiController]
[Route("api/v1/payroll")]
[Authorize]
public sealed class PayrollController : ApiControllerBase
{
    private const string PayrollAdmins = nameof(UserRole.Finance) + "," + nameof(UserRole.HrAdmin);

    private readonly ISender _sender;

    public PayrollController(ISender sender) => _sender = sender;

    // ---- Payslips (synchronous) ------------------------------------------

    /// <summary>Generates (or returns) an employee's payslip for a period. Finance / HR Admin.</summary>
    [HttpPost("payslips")]
    [Authorize(Roles = PayrollAdmins)]
    public async Task<ActionResult<PayslipDto>> GeneratePayslip(GeneratePayslipCommand command, CancellationToken cancellationToken)
        => Ok(await _sender.Send(command, cancellationToken));

    /// <summary>Lists a given employee's payslips. Finance / HR Admin.</summary>
    [HttpGet("payslips")]
    [Authorize(Roles = PayrollAdmins)]
    public async Task<ActionResult<IReadOnlyList<PayslipDto>>> GetPayslips(
        [FromQuery] Guid employeeId,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetPayslipsByEmployeeQuery(employeeId), cancellationToken));

    /// <summary>Lists the current user's own payslips.</summary>
    [HttpGet("payslips/me")]
    public async Task<ActionResult<IReadOnlyList<PayslipDto>>> GetMyPayslips(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetPayslipsByEmployeeQuery(CurrentUserId), cancellationToken));

    // ---- Settlements (asynchronous) --------------------------------------

    /// <summary>Queues an asynchronous payroll settlement. Returns 202 with a status URL. Finance only.</summary>
    [HttpPost("settlements")]
    [Authorize(Roles = nameof(UserRole.Finance))]
    public async Task<ActionResult<SettlementRequestDto>> RequestSettlement(
        RequestSettlementCommand command,
        CancellationToken cancellationToken)
    {
        var settlement = await _sender.Send(command, cancellationToken);
        return AcceptedAtAction(nameof(GetSettlement), new { id = settlement.Id }, settlement);
    }

    /// <summary>Gets the current status of an async settlement. Finance / HR Admin.</summary>
    [HttpGet("settlements/{id:guid}")]
    [Authorize(Roles = PayrollAdmins)]
    public async Task<ActionResult<SettlementRequestDto>> GetSettlement(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetSettlementByIdQuery(id), cancellationToken));
}
