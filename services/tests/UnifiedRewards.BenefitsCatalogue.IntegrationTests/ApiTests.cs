using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UnifiedRewards.BenefitsCatalogue.IntegrationTests;

public sealed class BenefitPlansApiTests : IClassFixture<ServiceFixture>
{
    private readonly HttpClient _client;

    public BenefitPlansApiTests(ServiceFixture factory) => _client = factory.CreateClient();

    private void Auth(string role) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenFactory.ForRole(role));

    [Fact]
    public async Task GetPlans_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/benefit-plans")).StatusCode);
    }

    [Fact]
    public async Task GetPlans_AnyRole_Returns200WithSeededData()
    {
        Auth("Employee");
        var resp = await _client.GetAsync("/api/benefit-plans?activeOnly=false");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        // Program.cs seeds 3 plans; other tests in this class may add more to the shared DB.
        Assert.True(json.RootElement.GetProperty("totalCount").GetInt32() >= 3);
    }

    [Fact]
    public async Task CreatePlan_HrAdmin_Returns201()
    {
        Auth("HrAdmin");
        var resp = await _client.PostAsJsonAsync("/api/benefit-plans", new
        {
            name = $"Plan-{Guid.NewGuid():N}",
            description = "Test plan",
            category = 0,  // Insurance
            monthlyCost = 500m
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task CreatePlan_DuplicateName_Returns409()
    {
        Auth("HrAdmin");
        var name = $"DupPlan-{Guid.NewGuid():N}";
        var body = new { name, description = "Desc", category = 1, monthlyCost = 100m };
        await _client.PostAsJsonAsync("/api/benefit-plans", body);
        var resp = await _client.PostAsJsonAsync("/api/benefit-plans", body);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task CreatePlan_NonHrAdmin_Returns403()
    {
        Auth("Employee");
        var resp = await _client.PostAsJsonAsync("/api/benefit-plans", new
        {
            name = $"UnauthorisedPlan-{Guid.NewGuid():N}",
            description = "Should fail",
            category = 0,
            monthlyCost = 100m
        });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }
}

public sealed class EnrollmentsApiTests : IClassFixture<ServiceFixture>
{
    private readonly HttpClient _client;

    public EnrollmentsApiTests(ServiceFixture factory) => _client = factory.CreateClient();

    private void Auth(string role, Guid? userId = null) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenFactory.ForRole(role, userId: userId));

    private async Task<Guid> GetFirstPlanIdAsync()
    {
        Auth("Employee");
        var resp = await _client.GetAsync("/api/benefit-plans?activeOnly=false");
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        return Guid.Parse(json.RootElement.GetProperty("items")[0].GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task Enroll_Employee_Returns201()
    {
        var planId = await GetFirstPlanIdAsync();
        var userId = Guid.NewGuid();
        Auth("Employee", userId);

        var resp = await _client.PostAsJsonAsync("/api/enrollments", new
        {
            benefitPlanId = planId,
            coverageStartDate = "2024-07-01"
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task GetMine_ReturnsOwnEnrollments()
    {
        var planId = await GetFirstPlanIdAsync();
        var userId = Guid.NewGuid();
        Auth("Employee", userId);

        await _client.PostAsJsonAsync("/api/enrollments", new
        {
            benefitPlanId = planId,
            coverageStartDate = "2024-08-01"
        });

        var resp = await _client.GetAsync("/api/enrollments/me");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.True(json.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Enroll_NonExistentPlan_Returns400()
    {
        Auth("Employee");
        var resp = await _client.PostAsJsonAsync("/api/enrollments", new
        {
            benefitPlanId = Guid.NewGuid(),
            coverageStartDate = "2024-07-01"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
