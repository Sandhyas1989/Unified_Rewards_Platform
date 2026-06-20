using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UnifiedRewards.ReimbursementWorkflow.IntegrationTests;

public sealed class ClaimsApiTests : IClassFixture<ServiceFixture>
{
    private readonly HttpClient _client;

    public ClaimsApiTests(ServiceFixture factory) => _client = factory.CreateClient();

    private void Auth(string role, Guid? userId = null) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenFactory.ForRole(role, userId: userId));

    [Fact]
    public async Task Submit_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.PostAsJsonAsync("/api/claims", new { type = 0, amount = 100m, description = "test" })).StatusCode);
    }

    [Fact]
    public async Task Submit_Employee_Returns201WithSubmittedStatus()
    {
        Auth("Employee");
        var resp = await _client.PostAsJsonAsync("/api/claims", new
        {
            type = 0,       // Travel
            amount = 1500m,
            description = "Airport taxi"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.Equal(0, json.RootElement.GetProperty("status").GetInt32()); // Submitted
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("id").GetString()));
    }

    [Fact]
    public async Task Submit_NonEmployee_Returns403()
    {
        Auth("Finance");
        var resp = await _client.PostAsJsonAsync("/api/claims", new
        {
            type = 0,
            amount = 100m,
            description = "Finance cannot submit"
        });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task GetMine_Employee_Returns200List()
    {
        var userId = Guid.NewGuid();
        Auth("Employee", userId);
        await _client.PostAsJsonAsync("/api/claims", new { type = 1, amount = 500m, description = "Medical" });

        var resp = await _client.GetAsync("/api/claims/me");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.True(json.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetList_Manager_Returns200Paged()
    {
        Auth("Manager");
        var resp = await _client.GetAsync("/api/claims");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.True(json.RootElement.TryGetProperty("items", out _));
    }

    [Fact]
    public async Task GetList_Employee_Returns403()
    {
        Auth("Employee");
        // Plain Employee cannot list all claims (only Manager/Finance/HrAdmin can)
        var resp = await _client.GetAsync("/api/claims");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Approve_ByManager_Returns200WithApprovedStatus()
    {
        // Submit as Employee
        Auth("Employee");
        var submitResp = await _client.PostAsJsonAsync("/api/claims", new
        {
            type = 2,
            amount = 800m,
            description = "Conference food"
        });
        var submitted = await JsonDocument.ParseAsync(await submitResp.Content.ReadAsStreamAsync());
        var claimId = submitted.RootElement.GetProperty("id").GetString()!;

        // Approve as Manager
        Auth("Manager");
        var approveResp = await _client.PostAsJsonAsync($"/api/claims/{claimId}/approve",
            new { notes = "Approved by test" });
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);
        var approved = await JsonDocument.ParseAsync(await approveResp.Content.ReadAsStreamAsync());
        Assert.Equal(2, approved.RootElement.GetProperty("status").GetInt32()); // Approved
    }

    [Fact]
    public async Task Reject_ByManager_Returns200WithRejectedStatus()
    {
        Auth("Employee");
        var submitResp = await _client.PostAsJsonAsync("/api/claims", new
        {
            type = 3,
            amount = 200m,
            description = "Internet bill"
        });
        var submitted = await JsonDocument.ParseAsync(await submitResp.Content.ReadAsStreamAsync());
        var claimId = submitted.RootElement.GetProperty("id").GetString()!;

        Auth("Manager");
        var rejectResp = await _client.PostAsJsonAsync($"/api/claims/{claimId}/reject",
            new { notes = "Does not qualify" });
        Assert.Equal(HttpStatusCode.OK, rejectResp.StatusCode);
        var rejected = await JsonDocument.ParseAsync(await rejectResp.Content.ReadAsStreamAsync());
        Assert.Equal(3, rejected.RootElement.GetProperty("status").GetInt32()); // Rejected
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        Auth("Manager");
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.GetAsync($"/api/claims/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Settle_NotApproved_Returns409()
    {
        // Submit but do NOT approve — settle should reject
        Auth("Employee");
        var submitResp = await _client.PostAsJsonAsync("/api/claims", new
        {
            type = 0,
            amount = 999m,
            description = "Settlement attempt on non-approved"
        });
        var submitted = await JsonDocument.ParseAsync(await submitResp.Content.ReadAsStreamAsync());
        var claimId = submitted.RootElement.GetProperty("id").GetString()!;

        Auth("Finance");
        var settleResp = await _client.PostAsync($"/api/claims/{claimId}/settle", null);
        Assert.Equal(HttpStatusCode.Conflict, settleResp.StatusCode);
    }
}
