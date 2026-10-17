using CarPrices.Domain.Entities;
using CarPrices.Domain.Enums;
using CarPrices.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarPrices.Statistics;

/// <summary>
/// Recomputes PriceStats rollups (percentiles by maker, maker+model, maker+model+year) from
/// captured sold lots. Percentiles are computed in C# (SQLite has none). PeriodType.All for now.
/// </summary>
public class RollupService(AppDbContext db, ILogger<RollupService> logger)
{
    private sealed record Row(long? MakerId, long? ModelId, int? Year, decimal Price, int? Odometer);

    public async Task<int> ComputeAsync(int minSample = 3, CancellationToken ct = default)
    {
        var sold = await db.Lots
            .Where(l => l.IsSold && l.SalePrice != null)
            .Include(l => l.Car)
            .ToListAsync(ct);

        var rows = sold.Where(l => l.Car != null)
            .Select(l => new Row(l.Car!.MakerId, l.Car.ModelId, l.Car.ModelYear, l.SalePrice!.Value, l.Odometer))
            .ToList();

        db.PriceStats.RemoveRange(await db.PriceStats.Where(p => p.PeriodType == PeriodType.All).ToListAsync(ct));

        var stats = new List<PriceStat>();
        var now = DateTime.UtcNow;

        void Emit(long? mk, long? md, int? yr, IReadOnlyCollection<Row> group)
        {
            var prices = group.Select(r => r.Price).ToList();
            if (prices.Count < minSample) return;
            var d = PercentileCalculator.Describe(prices)!;
            var odos = group.Where(r => r.Odometer.HasValue).Select(r => r.Odometer!.Value).ToList();
            stats.Add(new PriceStat
            {
                DimMakerId = mk,
                DimModelId = md,
                DimModelYear = yr,
                PeriodType = PeriodType.All,
                SampleSize = d.SampleSize,
                MinPrice = d.Min,
                MaxPrice = d.Max,
                MeanPrice = d.Mean,
                MedianPrice = d.Median,
                P25 = d.P25,
                P75 = d.P75,
                P90 = d.P90,
                StdDev = d.StdDev,
                MeanOdometer = odos.Count > 0 ? (int)Math.Round(odos.Average()) : null,
                ComputedAtUtc = now,
            });
        }

        foreach (var g in rows.Where(r => r.MakerId != null).GroupBy(r => r.MakerId))
            Emit(g.Key, null, null, g.ToList());

        foreach (var g in rows.Where(r => r.MakerId != null && r.ModelId != null).GroupBy(r => new { r.MakerId, r.ModelId }))
            Emit(g.Key.MakerId, g.Key.ModelId, null, g.ToList());

        foreach (var g in rows.Where(r => r.MakerId != null && r.ModelId != null && r.Year != null)
                     .GroupBy(r => new { r.MakerId, r.ModelId, r.Year }))
            Emit(g.Key.MakerId, g.Key.ModelId, g.Key.Year, g.ToList());

        db.PriceStats.AddRange(stats);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Rollup: {Count} PriceStat rows from {Sold} sold lots", stats.Count, rows.Count);
        return stats.Count;
    }
}
