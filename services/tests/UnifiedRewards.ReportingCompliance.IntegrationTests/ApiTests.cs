using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;

namespace UnifiedRewards.ReportingCompliance.IntegrationTests;

public sealed class AuditApiTests : IClassFixture<ServiceFixture>
{
    private readonly HttpClient _client;

    public AuditApiTests(ServiceFixture factory) => _client = factory.CreateClient();

    private void Auth(string role) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenFactory.ForRole(role));

    [Fact]
    public async Task GetAudit_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/audit")).StatusCode);
    }

    [Fact]
    public async Task GetAudit_Employee_Returns403()
    {
        Auth("Employee");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/audit")).StatusCode);
    }

    [Fact]
    public async Task GetAudit_HrAdmin_Returns200EmptyPaged()
    {
        Auth("HrAdmin");
        var resp = await _client.GetAsync("/api/audit");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.Equal(0, json.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, json.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task GetAudit_Finance_Returns200()
    {
        Auth("Finance");
        var resp = await _client.GetAsync("/api/audit");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetAudit_FilterByClaimId_Returns200EmptyList()
    {
        Auth("HrAdmin");
        var claimId = Guid.NewGuid();
        var resp = await _client.GetAsync($"/api/audit?claimId={claimId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.Equal(0, json.RootElement.GetProperty("totalCount").GetInt32());
    }
}
