using System.Text.RegularExpressions;
using CarPrices.Domain.Enums;
using CarPrices.Domain.Vin;
using CarPrices.Vision.Barcode;
using CarPrices.Vision.Ocr;

namespace CarPrices.Vision;

public record VinCandidate(string Vin, VinExtractionMethod Method, string Engine, bool CheckDigitValid, double Confidence);

/// <summary>
/// Pulls VIN candidates from an image (barcode first, OCR fallback) and selects the best one that is
/// check-digit-valid AND consistent with the masked VIN from the listing. Pure/testable (image
/// decoding is behind <see cref="IBarcodeReader"/> / <see cref="IOcrEngine"/>).
/// </summary>
public partial class VinImageExtractor(IBarcodeReader barcode, IOcrEngine ocr)
{
    [GeneratedRegex("[A-HJ-NPR-Z0-9]{17}")]
    private static partial Regex VinToken();

    public IReadOnlyList<VinCandidate> ExtractCandidates(byte[] imageBytes)
    {
        var list = new List<VinCandidate>();
        foreach (var text in barcode.ReadBarcodes(imageBytes))
            foreach (var vin in Tokens(text))
                list.Add(Make(vin, VinExtractionMethod.Barcode, "ZXing"));

        foreach (var vin in Tokens(ocr.ReadText(imageBytes)))
            list.Add(Make(vin, VinExtractionMethod.Ocr, ocr.Name));

        return list;
    }

    /// <summary>Best valid candidate consistent with the masked VIN prefix; barcode wins ties.</summary>
    public VinCandidate? BestCandidate(IEnumerable<VinCandidate> candidates, string? maskedVisible) =>
        candidates
            .Where(c => c.CheckDigitValid && VinValidator.MatchesMaskedPattern(c.Vin, maskedVisible))
            .OrderByDescending(c => c.Method == VinExtractionMethod.Barcode)
            .ThenByDescending(c => c.Confidence)
            .FirstOrDefault();

    private static IEnumerable<string> Tokens(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        return VinToken().Matches(text.ToUpperInvariant()).Select(m => m.Value).Distinct();
    }

    private static VinCandidate Make(string vin, VinExtractionMethod method, string engine)
    {
        var valid = VinValidator.CheckDigitValid(vin);
        var conf = method == VinExtractionMethod.Barcode ? (valid ? 0.98 : 0.60) : (valid ? 0.85 : 0.45);
        return new VinCandidate(vin, method, engine, valid, conf);
    }
}
