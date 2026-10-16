namespace CarPrices.Vision.Barcode;

/// <summary>Reads barcode payloads from an encoded image (PNG/JPEG bytes).</summary>
public interface IBarcodeReader
{
    IEnumerable<string> ReadBarcodes(byte[] imageBytes);
}
