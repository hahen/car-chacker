using CarPrices.Domain.Enums;
using CarPrices.Vision;
using CarPrices.Vision.Barcode;
using CarPrices.Vision.Ocr;

namespace CarPrices.Tests;

public class VinImageExtractorTests
{
    private sealed class FakeBarcode(string? payload) : IBarcodeReader
    {
        public IEnumerable<string> ReadBarcodes(byte[] imageBytes) =>
            payload is null ? Array.Empty<string>() : new[] { payload };
    }

    [Fact]
    public void Extracts_valid_vin_from_barcode()
    {
        var ext = new VinImageExtractor(new FakeBarcode("I|1HGCM82633A004352|LOT123"), new NullOcrEngine());
        var c = Assert.Single(ext.ExtractCandidates(new byte[] { 1 }));
        Assert.Equal("1HGCM82633A004352", c.Vin);
        Assert.True(c.CheckDigitValid);
        Assert.Equal(VinExtractionMethod.Barcode, c.Method);
    }

    [Fact]
    public void Best_candidate_requires_matching_masked_pattern()
    {
        var ext = new VinImageExtractor(new FakeBarcode("1HGCM82633A004352"), new NullOcrEngine());
        var cands = ext.ExtractCandidates(new byte[] { 1 });
        Assert.Equal("1HGCM82633A004352", ext.BestCandidate(cands, "1HGCM82633A")!.Vin);
        Assert.Null(ext.BestCandidate(cands, "ZZZZZZZZZZZ"));
    }

    [Fact]
    public void No_barcode_yields_no_candidates()
    {
        var ext = new VinImageExtractor(new FakeBarcode(null), new NullOcrEngine());
        Assert.Empty(ext.ExtractCandidates(new byte[] { 1 }));
    }
}
