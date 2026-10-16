using CarPrices.Domain.Enums;

namespace CarPrices.Domain.Entities;

/// <summary>A grouping of features, e.g. Safety, Comfort, Infotainment.</summary>
public class FeatureCategory
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Feature> Features { get; set; } = new List<Feature>();
}

/// <summary>A canonical, normalized feature, e.g. ADAPTIVE_CRUISE, CAMERA_360, AWD.</summary>
public class Feature
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public long? CategoryId { get; set; }
    public FeatureCategory? Category { get; set; }
    public string? Description { get; set; }

    public ICollection<CarFeature> CarFeatures { get; set; } = new List<CarFeature>();
    public ICollection<FeatureMapping> Mappings { get; set; } = new List<FeatureMapping>();
}

/// <summary>A feature applied to a specific car, with how it was determined.</summary>
public class CarFeature
{
    public long Id { get; set; }
    public long CarId { get; set; }
    public Car? Car { get; set; }
    public long FeatureId { get; set; }
    public Feature? Feature { get; set; }
    public FeatureSource Source { get; set; }
    public double Confidence { get; set; } = 1.0;
    public DateTime FirstSeenAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Raw option string from a source, before mapping to a canonical Feature.</summary>
public class RawOption
{
    public long Id { get; set; }
    public long? CarId { get; set; }
    public Car? Car { get; set; }
    public string? VinPattern { get; set; }
    public long? MakerId { get; set; }
    public Maker? Maker { get; set; }
    public string? RawCode { get; set; }
    public string RawText { get; set; } = string.Empty;
    public FeatureSource Source { get; set; }
}

/// <summary>A rule mapping raw option text/codes to a canonical Feature (per-maker or global).</summary>
public class FeatureMapping
{
    public long Id { get; set; }
    public long? MakerId { get; set; }
    public Maker? Maker { get; set; }
    public FeatureMatchType MatchType { get; set; } = FeatureMatchType.Contains;
    public string Pattern { get; set; } = string.Empty;
    public long FeatureId { get; set; }
    public Feature? Feature { get; set; }
    public int Priority { get; set; }
}
