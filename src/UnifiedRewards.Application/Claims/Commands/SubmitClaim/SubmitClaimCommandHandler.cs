using System.Globalization;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UnifiedRewards.Application.Claims.Dtos;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Application.Common.Interfaces.Integration;
using UnifiedRewards.Domain.Claims;

namespace UnifiedRewards.Application.Claims.Commands.SubmitClaim;

public sealed class SubmitClaimCommandHandler : IRequestHandler<SubmitClaimCommand, ClaimDto>
{
    private static readonly Regex AmountPattern = new(@"\d+(?:\.\d{1,2})?", RegexOptions.Compiled);

    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _fileStorage;
    private readonly IOcrEngine _ocrEngine;
    private readonly ILogger<SubmitClaimCommandHandler> _logger;

    public SubmitClaimCommandHandler(
        IApplicationDbContext db,
        IFileStorage fileStorage,
        IOcrEngine ocrEngine,
        ILogger<SubmitClaimCommandHandler> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _ocrEngine = ocrEngine;
        _logger = logger;
    }

    public async Task<ClaimDto> Handle(SubmitClaimCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == request.EmployeeId, cancellationToken))
        {
            throw new NotFoundException("User", request.EmployeeId);
        }

        var claim = Claim.Submit(request.EmployeeId, request.Type, request.Amount, request.Description);

        if (request.Receipt is { Length: > 0 } receipt)
        {
            var fileName = string.IsNullOrWhiteSpace(request.ReceiptFileName) ? "receipt" : request.ReceiptFileName!;
            var contentType = string.IsNullOrWhiteSpace(request.ReceiptContentType)
                ? "application/octet-stream"
                : request.ReceiptContentType!;

            claim.ReceiptReference = await _fileStorage.UploadAsync(
                fileName, new MemoryStream(receipt), contentType, cancellationToken);
            claim.ReceiptFileName = fileName;
            claim.ReceiptContentType = contentType;

            // OCR is best-effort: a scan failure must not block claim submission.
            try
            {
                var ocr = await _ocrEngine.ExtractAsync(new MemoryStream(receipt), cancellationToken);
                claim.OcrText = ocr.Text;
                claim.OcrConfidence = ocr.Confidence;
                claim.OcrExtractedAmount = TryExtractAmount(ocr.Text);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OCR extraction failed for claim receipt; continuing without OCR data.");
            }
        }

        _db.Claims.Add(claim);
        await _db.SaveChangesAsync(cancellationToken);

        return claim.ToDto();
    }

    /// <summary>Picks the largest numeric token from the OCR text as a best-guess receipt total.</summary>
    private static decimal? TryExtractAmount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        decimal? max = null;
        foreach (Match m in AmountPattern.Matches(text))
        {
            if (decimal.TryParse(m.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                && (max is null || value > max))
            {
                max = value;
            }
        }

        return max;
    }
}
