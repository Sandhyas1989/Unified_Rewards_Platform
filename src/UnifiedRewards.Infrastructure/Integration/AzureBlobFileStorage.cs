using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using UnifiedRewards.Application.Common.Interfaces.Integration;

namespace UnifiedRewards.Infrastructure.Integration;

/// <summary>
/// Production document store backed by Azure Blob Storage. Enabled by configuration
/// ("Storage:Provider" = "Blob"); the default local profile writes to the file system.
/// </summary>
public sealed class AzureBlobFileStorage : IFileStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobFileStorage(IConfiguration configuration)
    {
        var connectionString = configuration["Storage:Blob:ConnectionString"]
            ?? throw new InvalidOperationException("Storage:Blob:ConnectionString is required when Storage:Provider=Blob.");
        var containerName = configuration["Storage:Blob:Container"] ?? "receipts";
        _container = new BlobContainerClient(connectionString, containerName);
        _container.CreateIfNotExists();
    }

    public async Task<string> UploadAsync(string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var reference = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var blob = _container.GetBlobClient(reference);
        await blob.UploadAsync(content, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } }, cancellationToken);
        return reference;
    }

    public async Task<Stream> DownloadAsync(string reference, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(reference);
        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }
}
