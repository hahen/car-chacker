using CarPrices.Domain.Entities;
using CarPrices.Domain.Enums;
using CarPrices.Enrichment.Features;
using CarPrices.Enrichment.Vpic;
using Microsoft.EntityFrameworkCore;

namespace CarPrices.Tests;

public class FeatureExtractorTests
{
    private static VpicResult Vpic(params (string Key, string Value)[] kv)
    {
        var d = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in kv) d[k] = v;
        return new VpicResult(d);
    }

    private static async Task<Car> NewCarAsync(TestDb t)
    {
        var car = new Car();
        t.Db.Cars.Add(car);
        await t.Db.SaveChangesAsync();
        return car;
    }

    [Fact]
    public async Task Extracts_vpic_structured_features()
    {
        using var t = new TestDb();
        var car = await NewCarAsync(t);
        var fx = new FeatureExtractor(t.Db);

        var added = await fx.ExtractFromVpicAsync(car, Vpic(
            ("AdaptiveCruiseControl", "Standard"),
            ("RearVisibilitySystem", "Backup Camera"),
            ("KeylessIgnition", "Standard"),
            ("DriveType", "AWD/All-Wheel Drive")));

        var codes = await t.Db.CarFeatures.Include(f => f.Feature)
            .Where(f => f.CarId == car.Id).Select(f => f.Feature!.Code).ToListAsync();

        Assert.Equal(5, added);
        Assert.Contains("ADAPTIVE_CRUISE", codes);
        Assert.Contains("BACKUP_CAMERA", codes);
        Assert.Contains("KEYLESS_ENTRY", codes);
        Assert.Contains("PUSH_BUTTON_START", codes);
        Assert.Contains("AWD", codes);
    }

    [Fact]
    public async Task Extracts_text_features_via_mappings()
    {
        using var t = new TestDb();
        var car = await NewCarAsync(t);
        var fx = new FeatureExtractor(t.Db);

        await fx.ExtractFromTextAsync(car, "Heated Seats, Navigation and Sunroof", FeatureSource.ListingText);

        var codes = await t.Db.CarFeatures.Include(f => f.Feature)
            .Where(f => f.CarId == car.Id).Select(f => f.Feature!.Code).ToListAsync();
        Assert.Contains("HEATED_SEATS", codes);
        Assert.Contains("NAVIGATION", codes);
        Assert.Contains("SUNROOF", codes);
    }

    [Fact]
    public async Task Is_idempotent()
    {
        using var t = new TestDb();
        var car = await NewCarAsync(t);
        var fx = new FeatureExtractor(t.Db);
        var r = Vpic(("DriveType", "AWD/All-Wheel Drive"));

        await fx.ExtractFromVpicAsync(car, r);
        var again = await fx.ExtractFromVpicAsync(car, r);

        Assert.Equal(0, again);
        Assert.Equal(1, await t.Db.CarFeatures.CountAsync(f => f.CarId == car.Id));
    }
}
