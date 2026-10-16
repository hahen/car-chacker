using CarPrices.Domain.Enums;

namespace CarPrices.Domain.Entities;

/// <summary>Precomputed price rollup for a dimension combination over a period.</summary>
public class PriceStat
{
    public long Id { get; set; }

    public long? DimMakerId { get; set; }
    public long? DimModelId { get; set; }
    public long? DimTrimId { get; set; }
    public int? DimModelYear { get; set; }
    public TitleGroup? DimTitleGroup { get; set; }
    public DamageGroup? DimDamageGroup { get; set; }
    public string? DimRegion { get; set; }

    public PeriodType PeriodType { get; set; } = PeriodType.All;
    public string? PeriodKey { get; set; }

    public int SampleSize { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MeanPrice { get; set; }
    public decimal? MedianPrice { get; set; }
    public decimal? P25 { get; set; }
    public decimal? P75 { get; set; }
    public decimal? P90 { get; set; }
    public decimal? StdDev { get; set; }
    public int? MeanOdometer { get; set; }

    public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Estimated price impact of a feature within a segment.</summary>
public class FeaturePriceImpact
{
    public long Id { get; set; }
    public long FeatureId { get; set; }
    public Feature? Feature { get; set; }
    public string SegmentKey { get; set; } = string.Empty;
    public int WithN { get; set; }
    public int WithoutN { get; set; }
    public decimal? MeanWith { get; set; }
    public decimal? MeanWithout { get; set; }
    public decimal? DeltaAbs { get; set; }
    public double? DeltaPct { get; set; }
    public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>A daily median-price time-series point for a segment.</summary>
public class PriceIndexDaily
{
    public long Id { get; set; }
    public string SegmentKey { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal? MedianPrice { get; set; }
    public int SampleSize { get; set; }
}

/// <summary>A row from the imported VinCheck aggregate auction price index.</summary>
public class VinCheckIndex
{
    public long Id { get; set; }
    public int ModelYear { get; set; }
    public string Maker { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int SampleSize { get; set; }
    public decimal? P25 { get; set; }
    public decimal? P50 { get; set; }
    public decimal? P75 { get; set; }
    public DateTime? GeneratedAtUtc { get; set; }
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    public Source Source { get; set; } = Source.VinCheck;
}
