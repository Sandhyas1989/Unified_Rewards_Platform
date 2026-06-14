namespace UnifiedRewards.DocumentProcessing.Storage;

// File-storage seam (ported from the monolith's IFileStorage). Local filesystem by default;
// Azure Blob Storage is the production swap (selected by config), same as the monolith.
public interface IFileStorage
{
    Task<string> UploadAsync(string fileName, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string reference, CancellationToken ct = default);
}

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage()
    {
        // Deterministic location under the app base dir (independent of the launch working directory).
        _root = Path.Combine(AppContext.BaseDirectory, "App_Data", "receipts");
        Directory.CreateDirectory(_root);
    }

    public async Task<string> UploadAsync(string fileName, Stream content, string contentType, CancellationToken ct = default)
    {
        var reference = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        await using var fs = File.Create(Path.Combine(_root, reference));
        await content.CopyToAsync(fs, ct);
        return reference;
    }

    public Task<Stream> DownloadAsync(string reference, CancellationToken ct = default)
        => Task.FromResult<Stream>(File.OpenRead(Path.Combine(_root, reference)));
}
