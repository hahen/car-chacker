using CarPrices.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarPrices.Statistics;

/// <summary>
/// Phase 0 statistics over the imported VinCheck aggregate index.
/// Later phases add rollups computed from captured Lots (sold prices) using PercentileCalculator.
/// </summary>
public class StatisticsService(AppDbContext db)
{
    public async Task<MarketOverviewDto> GetMarketOverviewAsync(CancellationToken ct = default)
    {
        var rows = await db.VinCheckIndex.AsNoTracking().ToListAsync(ct);
        if (rows.Count == 0)
            return new MarketOverviewDto(0, 0, 0, 0, null, null, null);

        var makes = rows.Select(r => r.Maker.ToLowerInvariant()).Distinct().Count();
        var models = rows.Select(r => (r.Maker.ToLowerInvariant(), r.Model.ToLowerInvariant())).Distinct().Count();
        var medians = rows.Where(r => r.P50.HasValue).Select(r => r.P50!.Value).ToList();
        decimal? medianPrice = medians.Count == 0 ? null : PercentileCalculator.Percentile(medians, 0.50);

        return new MarketOverviewDto(
            Makes: makes,
            Models: models,
            IndexRows: rows.Count,
            TotalObservations: rows.Sum(r => (long)r.SampleSize),
            MedianPrice: medianPrice,
            MinYear: rows.Min(r => r.ModelYear),
            MaxYear: rows.Max(r => r.ModelYear));
    }

    public async Task<List<MakeSummaryDto>> GetMakeSummariesAsync(CancellationToken ct = default)
    {
        var rows = await db.VinCheckIndex.AsNoTracking().ToListAsync(ct);
        return rows
            .GroupBy(r => r.Maker)
            .Select(g => new MakeSummaryDto(
                Make: g.Key,
                Models: g.Select(r => r.Model.ToLowerInvariant()).Distinct().Count(),
                Observations: g.Sum(r => (long)r.SampleSize),
                MedianPrice: g.Any(r => r.P50.HasValue)
                    ? PercentileCalculator.Percentile(g.Where(r => r.P50.HasValue).Select(r => r.P50!.Value), 0.50)
                    : null))
            .OrderByDescending(m => m.Observations)
            .ToList();
    }

    public async Task<List<string>> GetMakesAsync(CancellationToken ct = default) =>
        await db.VinCheckIndex.AsNoTracking()
            .Select(r => r.Maker).Distinct().OrderBy(m => m).ToListAsync(ct);

    public async Task<List<string>> GetModelsAsync(string make, CancellationToken ct = default)
    {
        var lower = make.ToLowerInvariant();
        return await db.VinCheckIndex.AsNoTracking()
            .Where(r => r.Maker.ToLower() == lower)
            .Select(r => r.Model).Distinct().OrderBy(m => m).ToListAsync(ct);
    }

    public async Task<ModelPriceSeriesDto> GetModelSeriesAsync(string make, string model, CancellationToken ct = default)
    {
        var lowerMake = make.ToLowerInvariant();
        var lowerModel = model.ToLowerInvariant();
        var points = await db.VinCheckIndex.AsNoTracking()
            .Where(r => r.Maker.ToLower() == lowerMake && r.Model.ToLower() == lowerModel)
            .OrderBy(r => r.ModelYear)
            .Select(r => new ModelYearPriceDto(r.ModelYear, r.SampleSize, r.P25, r.P50, r.P75))
            .ToListAsync(ct);
        return new ModelPriceSeriesDto(make, model, points);
    }
}
