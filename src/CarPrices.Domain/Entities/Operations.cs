using CarPrices.Domain.Enums;

namespace CarPrices.Domain.Entities;

/// <summary>Title type lookup, e.g. Salvage Certificate, Clean.</summary>
public class TitleType
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TitleGroup Group { get; set; } = TitleGroup.Unknown;
}

/// <summary>Damage type lookup, e.g. Front End, Rear End, Water/Flood.</summary>
public class DamageType
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DamageGroup Group { get; set; } = DamageGroup.Unknown;
}

/// <summary>Log of a scraping / enrichment / rollup job run.</summary>
public class ScrapeRun
{
    public long Id { get; set; }
    public Source Source { get; set; }
    public string JobType { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAtUtc { get; set; }
    public string Status { get; set; } = "Running";
    public int LotsSeen { get; set; }
    public int LotsNew { get; set; }
    public int LotsUpdated { get; set; }
    public int PricesCaptured { get; set; }
    public int ErrorCount { get; set; }
    public string? Notes { get; set; }

    public ICollection<IngestError> Errors { get; set; } = new List<IngestError>();
}

public class IngestError
{
    public long Id { get; set; }
    public long? ScrapeRunId { get; set; }
    public ScrapeRun? ScrapeRun { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string? SourceLotNumber { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>A configured collection scope (start filter) the scrapers run against.</summary>
public class CollectionTarget
{
    public long Id { get; set; }
    public string MakeText { get; set; } = string.Empty;
    public long? MakerId { get; set; }
    public Maker? Maker { get; set; }
    public string? ModelText { get; set; }
    public long? ModelId { get; set; }
    public Model? Model { get; set; }
    public string? TrimText { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public YearField YearField { get; set; } = YearField.Model;

    /// <summary>Comma-separated title groups (e.g. "Clean,Salvage"); null = any.</summary>
    public string? TitleGroups { get; set; }

    /// <summary>Comma-separated region/state codes; null = any.</summary>
    public string? Regions { get; set; }

    /// <summary>Comma-separated sources (e.g. "Copart,Iaai"); null = all enabled.</summary>
    public string? Sources { get; set; }

    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
