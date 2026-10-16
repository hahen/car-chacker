namespace CarPrices.Scraping.Collection;

/// <summary>Bound from the "Collection" config section — the scoped set of searches to run.</summary>
public class CollectionOptions
{
    public List<CollectionTargetConfig> Targets { get; set; } = new();
    public int MaxPagesPerSearch { get; set; } = 50;
    public bool IncludeUpcoming { get; set; } = true;
    public bool IncludeSold { get; set; } = true;
}

public class CollectionTargetConfig
{
    public string Make { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? Trim { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public string[]? Sources { get; set; }
    public string[]? TitleGroups { get; set; }
    public string[]? Regions { get; set; }
    public bool Enabled { get; set; } = true;
}
