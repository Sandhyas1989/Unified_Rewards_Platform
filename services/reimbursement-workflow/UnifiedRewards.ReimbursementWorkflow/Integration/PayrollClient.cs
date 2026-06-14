using System.Net.Http.Json;
using System.Text.Json;

namespace UnifiedRewards.ReimbursementWorkflow.Integration;

// Cross-service client for the Payroll Integration service. LOCALLY this is a direct HTTP call
// (orchestration saga); in PRODUCTION the same step is an asynchronous Azure Service Bus message.
public sealed class PayrollClient
{
    private readonly HttpClient _http;
    public PayrollClient(HttpClient http) => _http = http;

    public async Task<(Guid Id, string Reference)?> RequestSettlementAsync(
        Guid employeeId, decimal amount, string? authorization, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/settlements")
        {
            Content = JsonContent.Create(new { employeeId, amount }),
        };
        Forward(req, authorization);
        using var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        return (doc.RootElement.GetProperty("id").GetGuid(), doc.RootElement.GetProperty("reference").GetString()!);
    }

    public async Task<int?> GetSettlementStatusAsync(Guid id, string? authorization, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/settlements/{id}");
        Forward(req, authorization);
        using var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("status").GetInt32();
    }

    // Forward the caller's bearer token so the downstream service authorizes the action under the
    // same identity/tenant (the local stand-in for service-to-service auth; Entra in production).
    private static void Forward(HttpRequestMessage req, string? authorization)
    {
        if (!string.IsNullOrWhiteSpace(authorization))
            req.Headers.TryAddWithoutValidation("Authorization", authorization);
    }
}
