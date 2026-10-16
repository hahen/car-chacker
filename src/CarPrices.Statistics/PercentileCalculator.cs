namespace CarPrices.Statistics;

/// <summary>Price distribution computed in C# (SQLite has no native percentile functions).</summary>
public record PriceDistribution(
    int SampleSize,
    decimal Min,
    decimal Max,
    decimal Mean,
    decimal Median,
    decimal P25,
    decimal P75,
    decimal P90,
    decimal StdDev);

public static class PercentileCalculator
{
    /// <summary>Linear-interpolation percentile (p in [0,1]) over an unsorted sample.</summary>
    public static decimal Percentile(IEnumerable<decimal> values, double p)
    {
        var sorted = values.OrderBy(v => v).ToList();
        if (sorted.Count == 0) return 0m;
        if (sorted.Count == 1) return sorted[0];
        var rank = p * (sorted.Count - 1);
        var lo = (int)Math.Floor(rank);
        var hi = (int)Math.Ceiling(rank);
        if (lo == hi) return sorted[lo];
        var frac = (decimal)(rank - lo);
        return sorted[lo] + (sorted[hi] - sorted[lo]) * frac;
    }

    /// <summary>Full distribution summary; returns null for an empty sample.</summary>
    public static PriceDistribution? Describe(IEnumerable<decimal> values)
    {
        var list = values.ToList();
        if (list.Count == 0) return null;
        var mean = list.Average();
        var variance = list.Count == 1 ? 0m : list.Sum(v => (v - mean) * (v - mean)) / list.Count;
        var std = (decimal)Math.Sqrt((double)variance);
        return new PriceDistribution(
            SampleSize: list.Count,
            Min: list.Min(),
            Max: list.Max(),
            Mean: Math.Round(mean, 2),
            Median: Percentile(list, 0.50),
            P25: Percentile(list, 0.25),
            P75: Percentile(list, 0.75),
            P90: Percentile(list, 0.90),
            StdDev: Math.Round(std, 2));
    }
}
