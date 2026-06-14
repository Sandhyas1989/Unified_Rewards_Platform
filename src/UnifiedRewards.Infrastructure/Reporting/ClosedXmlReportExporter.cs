using ClosedXML.Excel;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Reporting.Dtos;

namespace UnifiedRewards.Infrastructure.Reporting;

/// <summary>ClosedXML implementation of the report exporter (.xlsx generation).</summary>
public sealed class ClosedXmlReportExporter : IReportExporter
{
    public byte[] BuildClaimsWorkbook(IReadOnlyList<ClaimReportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Claims");

        string[] headers = { "Claim Id", "Type", "Amount", "Status", "Submitted (UTC)", "Settled (UTC)" };
        for (var col = 0; col < headers.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = headers[col];
        }
        sheet.Row(1).Style.Font.Bold = true;

        var r = 2;
        foreach (var row in rows)
        {
            sheet.Cell(r, 1).Value = row.ClaimId.ToString();
            sheet.Cell(r, 2).Value = row.Type;
            sheet.Cell(r, 3).Value = row.Amount;
            sheet.Cell(r, 4).Value = row.Status;
            sheet.Cell(r, 5).Value = row.SubmittedAtUtc;
            sheet.Cell(r, 6).Value = row.SettledAtUtc;
            r++;
        }

        sheet.Column(3).Style.NumberFormat.Format = "#,##0.00";
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
