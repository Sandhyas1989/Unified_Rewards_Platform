using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UnifiedRewards.ReportingCompliance.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "HrAdmin,Finance")]
public sealed class ReportsController : ControllerBase
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private static readonly string[] ClaimStatus = { "Submitted", "UnderReview", "Approved", "Rejected", "Settled" };
    private static readonly string[] SettlementStatus = { "Pending", "Processing", "Succeeded", "Failed" };
    private static readonly string[] ClaimType = { "Travel", "Medical", "Food", "Internet", "Training", "Other" };

    private readonly IHttpClientFactory _factory;
    public ReportsController(IHttpClientFactory factory) => _factory = factory;

    // Calls a downstream service, forwarding the caller's token (the local stand-in for the
    // event-sourced read model; in production this service consumes Service Bus events instead).
    private async Task<string> FetchAsync(string clientName, string path, CancellationToken ct)
    {
        var client = _factory.CreateClient(clientName);
        var auth = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(auth)) client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", auth);
        return await client.GetStringAsync(path, ct);
    }

    private static List<(int Status, decimal Amount)> ParseRows(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var rows = new List<(int, decimal)>();
        foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
            rows.Add((item.GetProperty("status").GetInt32(), item.GetProperty("amount").GetDecimal()));
        return rows;
    }

    private static List<StatusAmountRow> Group(List<(int Status, decimal Amount)> rows, string[] labels) =>
        rows.GroupBy(r => r.Status)
            .Select(g => new StatusAmountRow(g.Key < labels.Length ? labels[g.Key] : g.Key.ToString(), g.Count(), g.Sum(x => x.Amount)))
            .OrderBy(r => r.Status).ToList();

    /// <summary>Cross-service operational dashboard, aggregated from Reimbursement + Payroll.</summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard(CancellationToken ct)
    {
        var claims = ParseRows(await FetchAsync("reimbursement", "/api/claims?pageSize=200", ct));
        var settlements = ParseRows(await FetchAsync("payroll", "/api/settlements?pageSize=200", ct));
        return Ok(new DashboardDto(
            DateTime.UtcNow,
            Group(claims, ClaimStatus),
            Group(settlements, SettlementStatus),
            claims.Count,
            settlements.Count,
            "Aggregated live via HTTP (local). In production this is an event-sourced read model fed by Azure Service Bus."));
    }

    /// <summary>Exports a claims report as an .xlsx workbook (ClosedXML).</summary>
    [HttpGet("claims/export")]
    public async Task<IActionResult> ExportClaims(CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(await FetchAsync("reimbursement", "/api/claims?pageSize=200", ct));
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Claims");
        ws.Cell(1, 1).Value = "Claim Id"; ws.Cell(1, 2).Value = "Type"; ws.Cell(1, 3).Value = "Amount";
        ws.Cell(1, 4).Value = "Status"; ws.Cell(1, 5).Value = "Submitted (UTC)";
        ws.Row(1).Style.Font.Bold = true;
        var r = 2;
        foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
        {
            var type = item.GetProperty("type").GetInt32();
            var status = item.GetProperty("status").GetInt32();
            ws.Cell(r, 1).Value = item.GetProperty("id").GetString();
            ws.Cell(r, 2).Value = type < ClaimType.Length ? ClaimType[type] : type.ToString();
            ws.Cell(r, 3).Value = item.GetProperty("amount").GetDecimal();
            ws.Cell(r, 4).Value = status < ClaimStatus.Length ? ClaimStatus[status] : status.ToString();
            ws.Cell(r, 5).Value = item.GetProperty("submittedAtUtc").GetString();
            r++;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), XlsxContentType, $"claims-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx");
    }
}
