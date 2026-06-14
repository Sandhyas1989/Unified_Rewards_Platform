using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tesseract;
using UnifiedRewards.Application.Common.Interfaces.Integration;

namespace UnifiedRewards.Infrastructure.Ocr;

/// <summary>
/// Real OCR engine backed by Tesseract. Enabled by configuration ("Ocr:Engine" = "Tesseract")
/// and requires a tessdata directory with the language data (e.g. eng.traineddata). The
/// default profile uses <c>StubOcrEngine</c> so the app runs with zero native dependencies.
/// </summary>
public sealed class TesseractOcrEngine : IOcrEngine
{
    private readonly string _tessDataPath;
    private readonly string _language;
    private readonly ILogger<TesseractOcrEngine> _logger;

    public TesseractOcrEngine(IConfiguration configuration, ILogger<TesseractOcrEngine> logger)
    {
        _tessDataPath = configuration["Ocr:TessDataPath"]
                        ?? Path.Combine(AppContext.BaseDirectory, "tessdata");
        _language = configuration["Ocr:Language"] ?? "eng";
        _logger = logger;
    }

    public async Task<OcrResult> ExtractAsync(Stream image, CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await image.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        // Tesseract's API is synchronous and its engine is not thread-safe, so we build a
        // short-lived engine per call and run it off the request thread.
        return await Task.Run(() =>
        {
            using var engine = new TesseractEngine(_tessDataPath, _language, EngineMode.Default);
            using var pix = Pix.LoadFromMemory(bytes);
            using var page = engine.Process(pix);

            var text = page.GetText();
            var confidence = (decimal)page.GetMeanConfidence();
            _logger.LogInformation("[OCR -> Tesseract] confidence={Confidence} chars={Length}", confidence, text.Length);
            return new OcrResult(text, confidence);
        }, cancellationToken);
    }
}
