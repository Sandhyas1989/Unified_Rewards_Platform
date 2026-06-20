using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;

namespace UnifiedRewards.DocumentProcessing.IntegrationTests;

public sealed class DocumentsApiTests : IClassFixture<ServiceFixture>
{
    private readonly HttpClient _client;

    public DocumentsApiTests(ServiceFixture factory) => _client = factory.CreateClient();

    private void Auth(string role) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenFactory.ForRole(role));

    private static MultipartFormDataContent BuildUploadForm(Guid claimId, string fileName = "receipt.pdf")
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(claimId.ToString()), "ClaimId");
        var fileContent = new ByteArrayContent("fake-pdf-bytes"u8.ToArray());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "File", fileName);
        return content;
    }

    [Fact]
    public async Task Upload_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        using var form = BuildUploadForm(Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.PostAsync("/api/documents", form)).StatusCode);
    }

    [Fact]
    public async Task Upload_AnyRole_Returns201WithDocumentDto()
    {
        Auth("Employee");
        var claimId = Guid.NewGuid();

        using var form = BuildUploadForm(claimId);
        var resp = await _client.PostAsync("/api/documents", form);

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.Equal(claimId.ToString(), json.RootElement.GetProperty("claimId").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("id").GetString()));
    }

    [Fact]
    public async Task GetList_AnyRole_Returns200Paged()
    {
        Auth("Employee");
        var resp = await _client.GetAsync("/api/documents");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.True(json.RootElement.TryGetProperty("items", out _));
    }

    [Fact]
    public async Task GetById_AfterUpload_Returns200()
    {
        Auth("Employee");
        using var form = BuildUploadForm(Guid.NewGuid(), "bill.pdf");
        var uploadResp = await _client.PostAsync("/api/documents", form);
        Assert.Equal(HttpStatusCode.Created, uploadResp.StatusCode);
        var created = await JsonDocument.ParseAsync(await uploadResp.Content.ReadAsStreamAsync());
        var id = created.RootElement.GetProperty("id").GetString()!;

        var getResp = await _client.GetAsync($"/api/documents/{id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var got = await JsonDocument.ParseAsync(await getResp.Content.ReadAsStreamAsync());
        Assert.Equal(id, got.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        Auth("Employee");
        var resp = await _client.GetAsync($"/api/documents/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Upload_NoFile_Returns400()
    {
        Auth("Employee");
        // Send form without any File part
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(Guid.NewGuid().ToString()), "ClaimId");
        var resp = await _client.PostAsync("/api/documents", content);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
