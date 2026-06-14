namespace UnifiedRewards.Application.Common.Interfaces.Integration;

/// <summary>
/// External-system seams. Each has a zero-install local implementation
/// (WireMock.Net, smtp4dev, local filesystem, Tesseract, MediatR notifications)
/// and an Azure implementation, selected by DI + ASPNETCORE_ENVIRONMENT.
/// Defined here so future modules (Payroll, Claims &amp; Documents, etc.)
/// code only against these abstractions.
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

public interface IPayrollService
{
    Task<bool> PushSettlementAsync(Guid employeeId, decimal amount, string reference, CancellationToken cancellationToken = default);
}

public interface IFileStorage
{
    Task<string> UploadAsync(string fileName, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(string reference, CancellationToken cancellationToken = default);
}

public sealed record OcrResult(string Text, decimal Confidence);

public interface IOcrEngine
{
    Task<OcrResult> ExtractAsync(Stream image, CancellationToken cancellationToken = default);
}

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
}
