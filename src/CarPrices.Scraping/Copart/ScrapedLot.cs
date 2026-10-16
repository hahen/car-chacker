namespace CarPrices.Scraping.Copart;

/// <summary>Normalized view of one Copart search-result lot, decoded from the raw short-key JSON.</summary>
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

    public DateTime? AuctionDateUtc { get; set; }
    public DateTime? LastAuctionDateUtc { get; set; }
    public int? SaleStatusCode { get; set; }   // ss
    public string? LotStatus { get; set; }      // lsts

    public string? YardName { get; set; }
    public string? LocationCity { get; set; }
    public string? LocationState { get; set; }
    public string? LocationCountry { get; set; }

    public int? Odometer { get; set; }
    public string? OdometerBrandCode { get; set; } // obc: A/N/E/X
    public string? Color { get; set; }
    public string? DriveType { get; set; }
    public string? FuelType { get; set; }
    public string? EngineDescription { get; set; }

    public string? TitleDescription { get; set; }  // td
    public string? TitleGroupCode { get; set; }     // tgc
    public string? PrimaryDamage { get; set; }      // dd
    public string? SecondaryDamage { get; set; }    // sdd
    public bool? HasKeys { get; set; }

    public List<string> ImageUrls { get; } = new();

    /// <summary>The raw per-lot JSON, retained for re-processing.</summary>
    public string RawJson { get; set; } = string.Empty;
}
