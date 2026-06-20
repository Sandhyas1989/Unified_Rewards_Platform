using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UnifiedRewards.CompensationRules.IntegrationTests;

public sealed class CompensationApiTests : IClassFixture<ServiceFixture>
{
    private readonly HttpClient _client;

    public CompensationApiTests(ServiceFixture factory) => _client = factory.CreateClient();

    private void Auth(string role) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenFactory.ForRole(role));

    [Fact]
    public async Task GetCompensation_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.GetAsync($"/api/compensation/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Generate_HrAdmin_Returns201WithComponents()
    {
        Auth("HrAdmin");
        var employeeId = Guid.NewGuid();

        var resp = await _client.PostAsJsonAsync("/api/compensation", new
        {
            employeeId,
            annualBasic = 600000m,
            grade = 1,         // GradeBand.L1 or similar
            effectiveFrom = "2024-04-01"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.True(json.RootElement.GetProperty("grossAnnual").GetDecimal() > 0);
        Assert.True(json.RootElement.GetProperty("components").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Generate_NonHrAdmin_Returns403()
    {
        Auth("Employee");
        var resp = await _client.PostAsJsonAsync("/api/compensation", new
        {
            employeeId = Guid.NewGuid(),
            annualBasic = 500000m,
            grade = 1,
            effectiveFrom = "2024-04-01"
        });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task GetById_AfterGenerate_Returns200()
    {
        Auth("HrAdmin");
        var createResp = await _client.PostAsJsonAsync("/api/compensation", new
        {
            employeeId = Guid.NewGuid(),
            annualBasic = 800000m,
            grade = 2,
            effectiveFrom = "2024-04-01"
        });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await JsonDocument.ParseAsync(await createResp.Content.ReadAsStreamAsync());
        var id = created.RootElement.GetProperty("id").GetString()!;

        var getResp = await _client.GetAsync($"/api/compensation/{id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var got = await JsonDocument.ParseAsync(await getResp.Content.ReadAsStreamAsync());
        Assert.Equal(id, got.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task Approve_AfterGenerate_Returns200Approved()
    {
        Auth("HrAdmin");
        var createResp = await _client.PostAsJsonAsync("/api/compensation", new
        {
            employeeId = Guid.NewGuid(),
            annualBasic = 700000m,
            grade = 1,
            effectiveFrom = "2024-04-01"
        });
        var created = await JsonDocument.ParseAsync(await createResp.Content.ReadAsStreamAsync());
        var id = created.RootElement.GetProperty("id").GetString()!;

        var approveResp = await _client.PostAsync($"/api/compensation/{id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);
        var approved = await JsonDocument.ParseAsync(await approveResp.Content.ReadAsStreamAsync());
        // status 1 = Approved
        Assert.Equal(1, approved.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task GetByEmployee_Returns200List()
    {
        Auth("HrAdmin");
        var employeeId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/compensation", new
        {
            employeeId,
            annualBasic = 500000m,
            grade = 1,
            effectiveFrom = "2024-04-01"
        });

        var resp = await _client.GetAsync($"/api/compensation?employeeId={employeeId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.True(json.RootElement.GetArrayLength() >= 1);
    }
}
