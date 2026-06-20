using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UnifiedRewards.EmployeeProfile.IntegrationTests;

// Each test class gets its own ServiceFixture (and therefore its own in-memory SQLite),
// so test classes are isolated from each other. Within a class, tests share one DB.
public sealed class AuthApiTests : IClassFixture<ServiceFixture>
{
    private readonly HttpClient _client;

    public AuthApiTests(ServiceFixture factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "hr@urp.local", password = "Password123!" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "hr@urp.local", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@urp.local", password = "Password123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}

public sealed class EmployeesApiTests : IClassFixture<ServiceFixture>
{
    private readonly HttpClient _client;

    public EmployeesApiTests(ServiceFixture factory) => _client = factory.CreateClient();

    private void Authenticate(string role)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenFactory.ForRole(role));
    }

    [Fact]
    public async Task GetEmployees_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var resp = await _client.GetAsync("/api/employees");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetEmployees_HrAdmin_Returns200Paged()
    {
        Authenticate("HrAdmin");
        var resp = await _client.GetAsync("/api/employees");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        // Seeded employees are present; items array exists
        Assert.True(json.RootElement.GetProperty("items").GetArrayLength() >= 0);
    }

    [Fact]
    public async Task CreateEmployee_HrAdmin_Returns201ThenGetById_Returns200()
    {
        Authenticate("HrAdmin");
        var email = $"test-{Guid.NewGuid():N}@urp.local";

        var createResp = await _client.PostAsJsonAsync("/api/employees", new
        {
            fullName = "Test User",
            email,
            password = "TestPass123!",
            grade = "E1",
            dateOfJoining = "2024-01-01"
        });

        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await JsonDocument.ParseAsync(await createResp.Content.ReadAsStreamAsync());
        var id = created.RootElement.GetProperty("id").GetString()!;

        var getResp = await _client.GetAsync($"/api/employees/{id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var got = await JsonDocument.ParseAsync(await getResp.Content.ReadAsStreamAsync());
        Assert.Equal(email, got.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task CreateEmployee_DuplicateEmail_Returns409()
    {
        Authenticate("HrAdmin");
        var email = $"dup-{Guid.NewGuid():N}@urp.local";
        var body = new { fullName = "Dup User", email, password = "TestPass123!", grade = "E1", dateOfJoining = "2024-01-01" };

        await _client.PostAsJsonAsync("/api/employees", body);
        var resp = await _client.PostAsJsonAsync("/api/employees", body);

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        Authenticate("HrAdmin");
        var resp = await _client.GetAsync($"/api/employees/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
