using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace CarPrices.Vision.Barcode;

/// <summary>
/// Decodes VIN barcodes (Code 39/128, Data Matrix, PDF417, QR) from an image using SkiaSharp for
/// pixel access and ZXing.Net for decoding. This is the primary, most reliable VIN-recovery path.
/// </summary>
public class BarcodeVinReader : IBarcodeReader
{
    private static readonly IList<BarcodeFormat> Formats = new List<BarcodeFormat>
    {
        BarcodeFormat.CODE_39,
        BarcodeFormat.CODE_128,
        BarcodeFormat.DATA_MATRIX,
        BarcodeFormat.PDF_417,
        BarcodeFormat.QR_CODE,
    };

    public IEnumerable<string> ReadBarcodes(byte[] imageBytes)
    {
        var found = new List<string>();
        if (imageBytes.Length == 0) return found;

        using var bmp = SKBitmap.Decode(imageBytes);
        if (bmp is null) return found;
        using var rgba = bmp.Copy(SKColorType.Rgba8888);
        if (rgba is null) return found;

        var source = new RGBLuminanceSource(rgba.Bytes, rgba.Width, rgba.Height, RGBLuminanceSource.BitmapFormat.RGBA32);
        var reader = new BarcodeReaderGeneric
        {
            Options = new DecodingOptions { TryHarder = true, PossibleFormats = Formats },
        };

        var results = reader.DecodeMultiple(source);
        if (results is not null)
            foreach (var r in results)
                if (!string.IsNullOrWhiteSpace(r.Text))
                    found.Add(r.Text);

        return found;
    }
}
