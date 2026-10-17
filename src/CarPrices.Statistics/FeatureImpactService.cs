using CarPrices.Domain.Entities;
using CarPrices.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarPrices.Statistics;

/// <summary>
/// Estimates each feature's price impact within a maker+model segment: mean sold price with vs
/// without the feature, over captured sold lots. Requires a minimum sample on both sides.
/// </summary>
public class FeatureImpactService(AppDbContext db, ILogger<FeatureImpactService> logger)
{
    public async Task<int> ComputeAsync(int minPerSide = 3, CancellationToken ct = default)
    {
        var sold = await db.Lots
            .Where(l => l.IsSold && l.SalePrice != null)
            .Include(l => l.Car).ThenInclude(c => c!.Features)
            .ToListAsync(ct);

        db.FeaturePriceImpacts.RemoveRange(await db.FeaturePriceImpacts.ToListAsync(ct));

        var impacts = new List<FeaturePriceImpact>();
        var now = DateTime.UtcNow;

        var segments = sold
            .Where(l => l.Car?.MakerId != null && l.Car?.ModelId != null)
            .GroupBy(l => new { MakerId = l.Car!.MakerId!.Value, ModelId = l.Car!.ModelId!.Value });

        foreach (var seg in segments)
        {
            var lots = seg.ToList();
            var segKey = $"maker:{seg.Key.MakerId}|model:{seg.Key.ModelId}";
            var featureIds = lots.SelectMany(l => l.Car!.Features.Select(f => f.FeatureId)).Distinct().ToList();

            foreach (var fid in featureIds)
            {
                var with = lots.Where(l => l.Car!.Features.Any(cf => cf.FeatureId == fid)).Select(l => l.SalePrice!.Value).ToList();
                var without = lots.Where(l => l.Car!.Features.All(cf => cf.FeatureId != fid)).Select(l => l.SalePrice!.Value).ToList();
                if (with.Count < minPerSide || without.Count < minPerSide) continue;

                var meanWith = with.Average();
                var meanWithout = without.Average();
                impacts.Add(new FeaturePriceImpact
                {
                    FeatureId = fid,
                    SegmentKey = segKey,
                    WithN = with.Count,
                    WithoutN = without.Count,
                    MeanWith = Math.Round(meanWith, 2),
                    MeanWithout = Math.Round(meanWithout, 2),
                    DeltaAbs = Math.Round(meanWith - meanWithout, 2),
                    DeltaPct = meanWithout != 0 ? (double)((meanWith - meanWithout) / meanWithout) : null,
                    ComputedAtUtc = now,
                });
            }
        }

        db.FeaturePriceImpacts.AddRange(impacts);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Feature impact: {Count} rows across segments", impacts.Count);
        return impacts.Count;
    }
}
