using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UnifiedRewards.PayrollIntegration.IntegrationTests;

public sealed class SettlementsApiTests : IClassFixture<ServiceFixture>
{
    private readonly HttpClient _client;

    public SettlementsApiTests(ServiceFixture factory) => _client = factory.CreateClient();

    private void Auth(string role) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenFactory.ForRole(role));

    [Fact]
    public async Task GetSettlements_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/settlements")).StatusCode);
    }

    [Fact]
    public async Task RequestSettlement_Finance_Returns202()
    {
        Auth("Finance");
        var resp = await _client.PostAsJsonAsync("/api/settlements", new
        {
            employeeId = Guid.NewGuid(),
            amount = 3500m
        });
        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    }

    [Fact]
    public async Task RequestSettlement_NonFinance_Returns403()
    {
        Auth("Employee");
        var resp = await _client.PostAsJsonAsync("/api/settlements", new
        {
            employeeId = Guid.NewGuid(),
            amount = 1000m
        });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task GetById_AfterRequest_Returns200()
    {
        Auth("Finance");
        var createResp = await _client.PostAsJsonAsync("/api/settlements", new
        {
            employeeId = Guid.NewGuid(),
            amount = 2200m
        });
        Assert.Equal(HttpStatusCode.Accepted, createResp.StatusCode);
        var created = await JsonDocument.ParseAsync(await createResp.Content.ReadAsStreamAsync());
        var id = created.RootElement.GetProperty("id").GetString()!;

        var getResp = await _client.GetAsync($"/api/settlements/{id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var got = await JsonDocument.ParseAsync(await getResp.Content.ReadAsStreamAsync());
        Assert.Equal(id, got.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetList_Finance_Returns200Paged()
    {
        Auth("Finance");
        var resp = await _client.GetAsync("/api/settlements");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.True(json.RootElement.TryGetProperty("items", out _));
    }
}

public sealed class PayslipsApiTests : IClassFixture<ServiceFixture>
{
    private readonly HttpClient _client;

    public PayslipsApiTests(ServiceFixture factory) => _client = factory.CreateClient();

    private void Auth(string role) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenFactory.ForRole(role));

    [Fact]
    public async Task GeneratePayslip_Finance_Returns201()
    {
        Auth("Finance");
        var resp = await _client.PostAsJsonAsync("/api/payslips", new
        {
            employeeId = Guid.NewGuid(),
            year = 2024,
            month = 6,
            grossMonthly = 60000m,
            totalDeductionsMonthly = 10000m,
            netMonthly = 50000m
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task GeneratePayslip_Idempotent_Returns200OnDuplicate()
    {
        Auth("Finance");
        var employeeId = Guid.NewGuid();
        var body = new { employeeId, year = 2024, month = 7, grossMonthly = 60000m, totalDeductionsMonthly = 10000m, netMonthly = 50000m };
        await _client.PostAsJsonAsync("/api/payslips", body);
        var resp = await _client.PostAsJsonAsync("/api/payslips", body);
        // Second call returns 200 (idempotent — returns existing)
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
