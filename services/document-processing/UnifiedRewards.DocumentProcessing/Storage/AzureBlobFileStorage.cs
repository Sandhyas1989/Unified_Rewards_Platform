using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace UnifiedRewards.DocumentProcessing.Storage;

// Azure Blob Storage swap for IFileStorage. Activated when Storage:Provider = AzureBlob in config.
// The container must exist before first use; Bicep creates it during infra deployment.
public sealed class AzureBlobFileStorage : IFileStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobFileStorage(string connectionString, string containerName)
    {
        _container = new BlobContainerClient(connectionString, containerName);
    }

    public async Task<string> UploadAsync(string fileName, Stream content, string contentType, CancellationToken ct = default)
    {
        var blobName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var blob = _container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } }, ct);
        return blobName;
    }

    public async Task<Stream> DownloadAsync(string reference, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(reference);
        var download = await blob.DownloadStreamingAsync(cancellationToken: ct);
        return download.Value.Content;
    }
}
