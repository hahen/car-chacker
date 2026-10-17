using CarPrices.Domain.Enums;
using CarPrices.Normalization;
using CarPrices.Scraping;
using CarPrices.Scraping.Ingestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarPrices.Tests;

public class LotIngestionServiceTests
{
    private static LotIngestionService Service(TestDb t) =>
        new(t.Db, new TaxonomyResolver(t.Db), NullLogger<LotIngestionService>.Instance);

    private static ScrapedLot Lot(string number, string? vin, DateTime? auction, decimal bid = 225m) => new()
    {
        LotNumber = number,
        Vin = vin,
        Make = "HYUNDAI",
        Model = "ELANTRA",
        Trim = "SE",
        Year = 2017,
        CurrentBid = bid,
        AuctionDateUtc = auction,
        Odometer = 94023,
        OdometerBrandCode = "A",
        Color = "RED",
        PrimaryDamage = "FRONT END",
        SecondaryDamage = "SIDE",
        TitleGroupCode = "TITLEGROUP_S",
        LocationState = "IL",
        RawJson = "{}",
    };

    [Fact]
    public async Task Ingest_creates_car_and_lot_with_resolved_fields()
    {
        using var t = new TestDb();
        var svc = Service(t);

        var outcome = await svc.IngestAsync(Lot("111", "KMHD84LF5HU******", DateTime.UtcNow.AddDays(3)), Source.Copart, null);

        Assert.True(outcome.IsNew);
        Assert.Equal(1, await t.Db.Cars.CountAsync());
        Assert.Equal(1, await t.Db.Lots.CountAsync());

        var lot = await t.Db.Lots.Include(l => l.Car).FirstAsync();
        Assert.Equal(225m, lot.CurrentBid);
        Assert.Equal(OdometerBrand.Actual, lot.OdometerBrand);
        Assert.NotNull(lot.TitleTypeId);
        Assert.NotNull(lot.PrimaryDamageId);
        Assert.Equal(2017, lot.Car!.ModelYear);
        Assert.NotNull(lot.Car.MakerId);
        Assert.Equal("KMHD84LF5HU", lot.Car.VinPattern);
        Assert.Null(lot.Car.Vin); // masked -> not stored as full VIN
    }

    [Fact]
    public async Task Ingest_is_idempotent_on_source_number_auctiondate()
    {
        using var t = new TestDb();
        var svc = Service(t);
        var auction = DateTime.UtcNow.AddDays(3);

        await svc.IngestAsync(Lot("111", null, auction, bid: 200m), Source.Copart, null);
        var second = await svc.IngestAsync(Lot("111", null, auction, bid: 350m), Source.Copart, null);

        Assert.False(second.IsNew);
        Assert.Equal(1, await t.Db.Cars.CountAsync());
        Assert.Equal(1, await t.Db.Lots.CountAsync());
        Assert.Equal(350m, (await t.Db.Lots.FirstAsync()).CurrentBid); // updated in place
    }

    [Fact]
    public async Task Same_full_vin_across_two_lots_shares_one_car()
    {
        using var t = new TestDb();
        var svc = Service(t);

        await svc.IngestAsync(Lot("111", "1HGCM82633A004352", DateTime.UtcNow.AddDays(3)), Source.Copart, null);
        await svc.IngestAsync(Lot("222", "1HGCM82633A004352", DateTime.UtcNow.AddDays(10)), Source.Copart, null);

        Assert.Equal(1, await t.Db.Cars.CountAsync());
        Assert.Equal(2, await t.Db.Lots.CountAsync());
        Assert.Equal("1HGCM82633A004352", (await t.Db.Cars.FirstAsync()).Vin);
    }
}
