using System.Collections.Concurrent;
using MediatR;
using Microsoft.Extensions.Logging;
using UnifiedRewards.Application.Common.Interfaces.Integration;

namespace UnifiedRewards.Infrastructure.Integration;

/// <summary>
/// Zero-install local-dev implementations of the integration seams. The Azure
/// implementations (Azure Communication Services, real payroll REST API,
/// Blob Storage, Azure Service Bus) are swapped in by DI in the cloud profile.
/// </summary>
public sealed class LocalEmailService : IEmailService
{
    private readonly ILogger<LocalEmailService> _logger;

    public LocalEmailService(ILogger<LocalEmailService> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        // Local dev: write to log; smtp4dev SMTP sink can be wired here later.
        _logger.LogInformation("[Email -> smtp4dev] To={To} Subject={Subject}", to, subject);
        return Task.CompletedTask;
    }
}

/// <summary>Thrown by the mock payroll system to simulate a retryable transient fault.</summary>
public sealed class TransientPayrollException : Exception
{
    public TransientPayrollException(string message) : base(message) { }
}

/// <summary>
/// Local stand-in for the external payroll system (WireMock.Net in full local dev). To exercise
/// the Polly resilience pipeline it fails the first push attempt for each reference with a
/// transient fault, then succeeds — so a single logical settlement is seen to retry and recover.
/// </summary>
public sealed class MockPayrollService : IPayrollService
{
    private static readonly ConcurrentDictionary<string, int> Attempts = new();
    private readonly ILogger<MockPayrollService> _logger;

    public MockPayrollService(ILogger<MockPayrollService> logger) => _logger = logger;

    public Task<bool> PushSettlementAsync(Guid employeeId, decimal amount, string reference, CancellationToken cancellationToken = default)
    {
        var attempt = Attempts.AddOrUpdate(reference, 1, (_, current) => current + 1);
        _logger.LogInformation(
            "[Payroll -> WireMock] attempt {Attempt} Employee={EmployeeId} Amount={Amount} Ref={Reference}",
            attempt, employeeId, amount, reference);

        if (attempt == 1)
        {
            throw new TransientPayrollException($"Transient payroll fault on attempt {attempt} for {reference}.");
        }

        return Task.FromResult(true);
    }
}

/// <summary>Local filesystem implementation of document/receipt storage (Azure Blob in cloud).</summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage()
    {
        _root = Path.Combine(AppContext.BaseDirectory, "App_Data", "files");
        Directory.CreateDirectory(_root);
    }

    public async Task<string> UploadAsync(string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var reference = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var path = Path.Combine(_root, reference);
        await using var fs = File.Create(path);
        await content.CopyToAsync(fs, cancellationToken);
        return reference;
    }

    public Task<Stream> DownloadAsync(string reference, CancellationToken cancellationToken = default)
        => Task.FromResult<Stream>(File.OpenRead(Path.Combine(_root, reference)));
}

/// <summary>Placeholder OCR engine. Replaced by a Tesseract.NET implementation in the Claims &amp; Documents module.</summary>
public sealed class StubOcrEngine : IOcrEngine
{
    public Task<OcrResult> ExtractAsync(Stream image, CancellationToken cancellationToken = default)
        => Task.FromResult(new OcrResult(string.Empty, 0m));
}

/// <summary>In-process event bus over MediatR notifications (Azure Service Bus in cloud).</summary>
public sealed class MediatrEventBus : IEventBus
{
    private readonly IPublisher _publisher;

    public MediatrEventBus(IPublisher publisher) => _publisher = publisher;

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
        => @event is INotification notification
            ? _publisher.Publish(notification, cancellationToken)
            : Task.CompletedTask;
}
