using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace UrpFunctions;

/// <summary>
/// The three serverless workloads in the deployment diagram. These are scaffolds that wire up the
/// triggers/bindings; the actual OCR / report / payroll logic is marked with TODOs (today that logic
/// lives inside the document-processing / reporting / payroll services).
///
/// App settings expected on the Function App:
///   AzureWebJobsStorage   = (the function app's storage account)
///   BlobConnection        = receipts storage account connection string
///   ServiceBusConnection  = Service Bus namespace connection string
/// </summary>
public class Functions
{
    private readonly ILogger _logger;
    public Functions(ILoggerFactory loggerFactory) => _logger = loggerFactory.CreateLogger<Functions>();

    /// <summary>Blob trigger — OCR on each uploaded receipt (diagram: "Blob: OCR trigger").</summary>
    [Function("OcrReceipt")]
    public void OcrReceipt(
        [BlobTrigger("receipts/{name}", Connection = "BlobConnection")] byte[] content,
        string name)
    {
        _logger.LogInformation("OCR trigger: receipt '{Name}' ({Size} bytes) uploaded.", name, content.Length);
        // TODO: run OCR (Tesseract/IronOCR) and POST the extracted text to document-processing.
    }

    /// <summary>Timer trigger — scheduled report generation (diagram: "Timer: Reports"). Daily 02:00 UTC.</summary>
    [Function("DailyReports")]
    public void DailyReports([TimerTrigger("0 0 2 * * *")] TimerInfo timer)
    {
        _logger.LogInformation("Timer trigger: generating scheduled reports at {Time:o}.", DateTime.UtcNow);
        // TODO: call reporting-compliance to materialise daily compliance reports.
    }

    /// <summary>Service Bus trigger — payroll batch (diagram: "Queue: Payroll"). Needs a 'payroll-functions' subscription on 'urp-events'.</summary>
    [Function("PayrollBatch")]
    public void PayrollBatch(
        [ServiceBusTrigger("urp-events", "payroll-functions", Connection = "ServiceBusConnection")] string message)
    {
        _logger.LogInformation("Service Bus trigger: payroll batch message received ({Length} chars).", message.Length);
        // TODO: invoke payroll-integration settlement processing.
    }
}
