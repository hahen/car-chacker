using CarPrices.Domain.Entities;
using CarPrices.Domain.Enums;
using CarPrices.Statistics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarPrices.Tests;

public class AnalyticsTests
{
    private static async Task<(long MakerId, long ModelId, long NavId)> IdsAsync(TestDb t)
    {
        var maker = await t.Db.Makers.FirstAsync(m => m.NormalizedName == "bmw");
        var model = await t.Db.Models.FirstAsync(m => m.NormalizedName == "3 series");
        var nav = await t.Db.Features.FirstAsync(f => f.Code == "NAVIGATION");
        return (maker.Id, model.Id, nav.Id);
    }

    private static async Task AddSoldAsync(TestDb t, long makerId, long modelId, int year, decimal price,
        long? featureId = null, int? odometer = null)
    {
        var car = new Car { MakerId = makerId, ModelId = modelId, ModelYear = year };
        if (featureId is long fid)
            car.Features.Add(new CarFeature { FeatureId = fid, Source = FeatureSource.VinDecode });
        t.Db.Cars.Add(car);
        await t.Db.SaveChangesAsync();

        t.Db.Lots.Add(new Lot
        {
            CarId = car.Id,
            Source = Source.Copart,
            SourceLotNumber = Guid.NewGuid().ToString("N")[..10],
            IsSold = true,
            SaleStatus = SaleStatus.Sold,
            SalePrice = price,
            Odometer = odometer,
        });
        await t.Db.SaveChangesAsync();
    }

    [Fact]
    public async Task Rollup_computes_stats_at_each_level()
    {
        using var t = new TestDb();
        var (mk, md, _) = await IdsAsync(t);
        foreach (var p in new[] { 10000m, 11000m, 12000m, 13000m, 14000m })
            await AddSoldAsync(t, mk, md, 2020, p);

        var n = await new RollupService(t.Db, NullLogger<RollupService>.Instance).ComputeAsync(minSample: 3);
        Assert.Equal(3, n); // maker, maker+model, maker+model+year

        var yearRow = await t.Db.PriceStats.FirstAsync(p => p.DimMakerId == mk && p.DimModelId == md && p.DimModelYear == 2020);
        Assert.Equal(5, yearRow.SampleSize);
        Assert.Equal(12000m, yearRow.MedianPrice);

        Assert.NotNull(await t.Db.PriceStats.FirstOrDefaultAsync(p => p.DimMakerId == mk && p.DimModelId == null));
    }

    [Fact]
    public async Task Rollup_skips_segments_below_min_sample()
    {
        using var t = new TestDb();
        var (mk, md, _) = await IdsAsync(t);
        await AddSoldAsync(t, mk, md, 2021, 10000m);
        await AddSoldAsync(t, mk, md, 2021, 11000m); // only 2 -> below min 3

        var n = await new RollupService(t.Db, NullLogger<RollupService>.Instance).ComputeAsync(minSample: 3);
        Assert.Equal(0, n);
    }

    [Fact]
    public async Task FeatureImpact_computes_delta()
    {
        using var t = new TestDb();
        var (mk, md, nav) = await IdsAsync(t);
        foreach (var p in new[] { 15000m, 16000m, 17000m }) await AddSoldAsync(t, mk, md, 2020, p, featureId: nav);
        foreach (var p in new[] { 10000m, 11000m, 12000m }) await AddSoldAsync(t, mk, md, 2020, p);

        var n = await new FeatureImpactService(t.Db, NullLogger<FeatureImpactService>.Instance).ComputeAsync(minPerSide: 3);
        Assert.Equal(1, n);

        var impact = await t.Db.FeaturePriceImpacts.FirstAsync(i => i.FeatureId == nav);
        Assert.Equal(3, impact.WithN);
        Assert.Equal(3, impact.WithoutN);
        Assert.Equal(16000m, impact.MeanWith);
        Assert.Equal(11000m, impact.MeanWithout);
        Assert.Equal(5000m, impact.DeltaAbs);
    }

    [Fact]
    public async Task Estimator_returns_comparable_distribution()
    {
        using var t = new TestDb();
        var (mk, md, _) = await IdsAsync(t);
        foreach (var p in new[] { 10000m, 11000m, 12000m, 13000m, 14000m })
            await AddSoldAsync(t, mk, md, 2020, p);

        var est = new EstimatorService(t.Db);
        var r = await est.EstimateAsync(new EstimateQuery(mk, md, null, 2020, YearTolerance: 1));
        Assert.Equal(5, r.Comparables);
        Assert.Equal(12000m, r.Median);
        Assert.Equal(12000m, r.Suggested);

        var none = await est.EstimateAsync(new EstimateQuery(mk, md, null, 1990, YearTolerance: 0));
        Assert.Equal(0, none.Comparables);
    }
}
