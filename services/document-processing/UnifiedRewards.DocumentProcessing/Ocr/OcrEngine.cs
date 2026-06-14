using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace UnifiedRewards.DocumentProcessing.Ocr;

public sealed record OcrResult(string Text, decimal Confidence, decimal? ExtractedAmount);

// OCR seam (ported from the monolith's IOcrEngine). The local/default engine reads text-based
// receipts and extracts the largest amount; Azure AI Document Intelligence / Tesseract is the
// production swap for image receipts (selected by config), same pattern as the monolith.
public interface IOcrEngine
{
    Task<OcrResult> ExtractAsync(byte[] content, CancellationToken ct = default);
}

public sealed class StubOcrEngine : IOcrEngine
{
    private static readonly Regex AmountPattern = new(@"\d+(?:\.\d{1,2})?", RegexOptions.Compiled);

    public Task<OcrResult> ExtractAsync(byte[] content, CancellationToken ct = default)
    {
        // Treat the upload as text if it is mostly printable (e.g. a .txt receipt). Real image
        // receipts return empty here and would be handled by the Tesseract/Azure engine in prod.
        var text = LooksLikeText(content) ? Encoding.UTF8.GetString(content) : string.Empty;
        decimal? max = null;
        foreach (Match m in AmountPattern.Matches(text))
        {
            if (decimal.TryParse(m.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)
                && (max is null || v > max))
            {
                max = v;
            }
        }
        var confidence = string.IsNullOrWhiteSpace(text) ? 0m : 0.95m;
        return Task.FromResult(new OcrResult(text, confidence, max));
    }

    private static bool LooksLikeText(byte[] bytes)
    {
        if (bytes.Length == 0) return false;
        var printable = bytes.Count(b => b == 9 || b == 10 || b == 13 || (b >= 32 && b < 127));
        return (double)printable / bytes.Length > 0.85;
    }
}
