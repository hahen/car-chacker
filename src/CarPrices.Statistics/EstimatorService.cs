using CarPrices.Domain.Entities;
using CarPrices.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarPrices.Statistics;

/// <summary>
/// Estimates a fair price from comparable captured sold lots (same maker/model/trim, year within a
/// tolerance, optional title/damage group), with an optional linear mileage adjustment.
/// </summary>
public class EstimatorService(AppDbContext db)
{
    public async Task<EstimateResult> EstimateAsync(EstimateQuery q, CancellationToken ct = default)
    {
        var lots = await db.Lots
            .Where(l => l.IsSold && l.SalePrice != null)
            .Include(l => l.Car)
            .Include(l => l.TitleType)
            .Include(l => l.PrimaryDamage)
            .ToListAsync(ct);

        var comps = lots.Where(l => Matches(l, q)).ToList();
        var prices = comps.Select(l => l.SalePrice!.Value).ToList();
        var d = PercentileCalculator.Describe(prices);
        if (d is null)
            return new EstimateResult(0, null, null, null, null, null, null, "no comparable sold lots");

        var mileageAdjusted = MileageAdjust(comps, q.Mileage, d.Median);
        return new EstimateResult(
            Comparables: d.SampleSize,
            Median: d.Median,
            P25: d.P25,
            P75: d.P75,
            Mean: d.Mean,
            Suggested: d.Median,
            MileageAdjusted: mileageAdjusted,
            Basis: $"median of {d.SampleSize} comparable sold lots");
    }

    private static bool Matches(Lot l, EstimateQuery q)
    {
        var car = l.Car;
        if (car is null) return false;
        if (q.MakerId is long mk && car.MakerId != mk) return false;
        if (q.ModelId is long md && car.ModelId != md) return false;
        if (q.TrimId is long tr && car.TrimId != tr) return false;
        if (q.Year is int yr && (car.ModelYear is null || Math.Abs(car.ModelYear.Value - yr) > q.YearTolerance)) return false;
        if (q.TitleGroup is { } tg && (l.TitleType is null || l.TitleType.Group != tg)) return false;
        if (q.DamageGroup is { } dg && (l.PrimaryDamage is null || l.PrimaryDamage.Group != dg)) return false;
        return true;
    }

    /// <summary>Linear mileage adjustment using the price-vs-odometer slope of the comparables.</summary>
    private static decimal? MileageAdjust(List<Lot> comps, int? targetMileage, decimal median)
    {
        if (targetMileage is null) return null;
        var pts = comps.Where(l => l.Odometer is > 0)
            .Select(l => ((double)l.Odometer!.Value, (double)l.SalePrice!.Value)).ToList();
        if (pts.Count < 5) return null;

        var slope = Slope(pts);
        var medOdo = Median(pts.Select(p => p.Item1).ToList());
        var adjusted = median + (decimal)(slope * (targetMileage.Value - medOdo));
        return adjusted < 0 ? 0 : Math.Round(adjusted, 0);
    }

    private static double Slope(List<(double X, double Y)> pts)
    {
        var n = pts.Count;
        double sx = pts.Sum(p => p.X), sy = pts.Sum(p => p.Y), sxx = pts.Sum(p => p.X * p.X), sxy = pts.Sum(p => p.X * p.Y);
        var denom = n * sxx - sx * sx;
        return Math.Abs(denom) < 1e-9 ? 0 : (n * sxy - sx * sy) / denom;
    }

    private static double Median(List<double> xs)
    {
        xs.Sort();
        var n = xs.Count;
        return n % 2 == 1 ? xs[n / 2] : (xs[n / 2 - 1] + xs[n / 2]) / 2;
    }
}
