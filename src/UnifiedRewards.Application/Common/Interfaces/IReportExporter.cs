using UnifiedRewards.Application.Reporting.Dtos;

namespace UnifiedRewards.Application.Common.Interfaces;

/// <summary>
/// Renders report data to file formats. Implemented in Infrastructure with ClosedXML so the
/// Application layer stays free of the spreadsheet dependency.
/// </summary>
public interface IReportExporter
{
    byte[] BuildClaimsWorkbook(IReadOnlyList<ClaimReportRow> rows);
}
