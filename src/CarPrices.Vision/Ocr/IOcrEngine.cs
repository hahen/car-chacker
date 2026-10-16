namespace CarPrices.Vision.Ocr;

/// <summary>Pluggable OCR backend. Default is a no-op; a Tesseract or cloud engine can be swapped in.</summary>
public interface IOcrEngine
{
    string Name { get; }
    string? ReadText(byte[] imageBytes);
}

/// <summary>Default OCR engine that reads nothing (barcode-only until a real engine is configured).</summary>
public sealed class NullOcrEngine : IOcrEngine
{
    public string Name => "none";
    public string? ReadText(byte[] imageBytes) => null;
}
