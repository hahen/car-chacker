namespace CarPrices.Scraping;

/// <summary>Source-agnostic normalized view of one auction lot, produced by a source parser
/// (Copart JSON, IAAI HTML, …) and consumed by the ingestion service.</summary>
public class ScrapedLot
{
    public string LotNumber { get; set; } = string.Empty;
    public string? Vin { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? Trim { get; set; }
    public int? Year { get; set; }

    public decimal? CurrentBid { get; set; }
    public decimal? BuyNowPrice { get; set; }
    public decimal? EstimatedRetailValue { get; set; }
    public string? Currency { get; set; }

    /// <summary>Confirmed final sale price (set by historical/archive sources).</summary>
    public decimal? SalePrice { get; set; }
    public bool IsSold { get; set; }

    public DateTime? AuctionDateUtc { get; set; }
    public DateTime? LastAuctionDateUtc { get; set; }
    public int? SaleStatusCode { get; set; }
    public string? LotStatus { get; set; }

    public string? YardName { get; set; }
    public string? LocationCity { get; set; }
    public string? LocationState { get; set; }
    public string? LocationCountry { get; set; }

    public int? Odometer { get; set; }
    public string? OdometerBrandCode { get; set; }
    public string? Color { get; set; }
    public string? DriveType { get; set; }
    public string? FuelType { get; set; }
    public string? EngineDescription { get; set; }

    public string? TitleDescription { get; set; }
    public string? TitleGroupCode { get; set; }
    public string? PrimaryDamage { get; set; }
    public string? SecondaryDamage { get; set; }
    public bool? HasKeys { get; set; }
    public string? RunAndDriveRaw { get; set; }
    public string? LotUrl { get; set; }

    public List<string> ImageUrls { get; } = new();

    /// <summary>The raw per-lot payload (JSON or HTML fragment), retained for re-processing.</summary>
    public string RawJson { get; set; } = string.Empty;
}
