using CarPrices.Domain.Enums;

namespace CarPrices.Domain.Entities;

/// <summary>A physical vehicle (one real car), identified by VIN where known.</summary>
public class Car
{
    public long Id { get; set; }

    /// <summary>Full 17-char VIN when known (may be recovered from images).</summary>
    public string? Vin { get; set; }

    /// <summary>First 11 chars of the VIN (WMI + VDS + check digit); decodes make/model/trim.</summary>
    public string? VinPattern { get; set; }

    public VinSource VinSource { get; set; } = VinSource.Unknown;
    public double? VinConfidence { get; set; }

    public long? MakerId { get; set; }
    public Maker? Maker { get; set; }
    public long? ModelId { get; set; }
    public Model? Model { get; set; }
    public long? TrimId { get; set; }
    public Trim? Trim { get; set; }

    /// <summary>Model-year designation (VIN position 10 / listing text).</summary>
    public int? ModelYear { get; set; }

    /// <summary>Actual production year (door-jamb build date / build data); can differ from ModelYear.</summary>
    public int? ManufactureYear { get; set; }

    public string? BodyStyle { get; set; }
    public string? DriveType { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public string? EngineDisplacement { get; set; }
    public int? EngineCylinders { get; set; }
    public string? EngineDescription { get; set; }
    public string? ExteriorColor { get; set; }

    public bool VinDecoded { get; set; }
    public DateTime? VinDecodedAtUtc { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Lot> Lots { get; set; } = new List<Lot>();
    public ICollection<CarFeature> Features { get; set; } = new List<CarFeature>();
}

/// <summary>A single auction appearance of a car (its own price/status/date).</summary>
public class Lot
{
    public long Id { get; set; }

    public long CarId { get; set; }
    public Car? Car { get; set; }

    public Source Source { get; set; }
    public string SourceLotNumber { get; set; } = string.Empty;
    public string? LotUrl { get; set; }

    public DateTime? AuctionDateUtc { get; set; }
    public string? SaleStatusRaw { get; set; }
    public SaleStatus SaleStatus { get; set; } = SaleStatus.Unknown;

    public string? YardCode { get; set; }
    public string? LocationCity { get; set; }
    public string? LocationState { get; set; }
    public string? LocationCountry { get; set; }

    public long? TitleTypeId { get; set; }
    public TitleType? TitleType { get; set; }
    public string? TitleState { get; set; }

    public long? PrimaryDamageId { get; set; }
    public DamageType? PrimaryDamage { get; set; }
    public long? SecondaryDamageId { get; set; }
    public DamageType? SecondaryDamage { get; set; }

    public int? Odometer { get; set; }
    public string? OdometerUnit { get; set; }
    public OdometerBrand OdometerBrand { get; set; } = OdometerBrand.Unknown;

    public bool? HasKeys { get; set; }
    public RunAndDrive RunAndDrive { get; set; } = RunAndDrive.Unknown;

    public decimal? EstimatedRetailValue { get; set; }
    public decimal? RepairCostEstimate { get; set; }

    public decimal? CurrentBid { get; set; }
    public decimal? BuyNowPrice { get; set; }
    public decimal? FinalBid { get; set; }

    /// <summary>Confirmed final sale price (captured at/after close). Null until known.</summary>
    public decimal? SalePrice { get; set; }
    public string? Currency { get; set; } = "USD";

    public bool IsSold { get; set; }
    public DateTime? SaleCapturedAtUtc { get; set; }
    public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;

    public long? RawPayloadId { get; set; }
    public RawPayload? RawPayload { get; set; }

    public ICollection<LotImage> Images { get; set; } = new List<LotImage>();
    public ICollection<BidHistory> Bids { get; set; } = new List<BidHistory>();
}

/// <summary>Raw source payload (JSON/HTML) retained so lots can be re-normalized without re-scraping.</summary>
public class RawPayload
{
    public long Id { get; set; }
    public Source Source { get; set; }
    public string? SourceLotNumber { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    public string ContentType { get; set; } = "application/json";
    public string Content { get; set; } = string.Empty;
}

/// <summary>A photo belonging to a lot; the VIN-recovery pipeline reads these.</summary>
public class LotImage
{
    public long Id { get; set; }
    public long LotId { get; set; }
    public Lot? Lot { get; set; }
    public string Url { get; set; } = string.Empty;
    public LotImageKind Kind { get; set; } = LotImageKind.Unknown;
    public int Sequence { get; set; }
    public string? LocalPath { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool OcrProcessed { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>A single observed bid on a lot (populated by live polling later).</summary>
public class BidHistory
{
    public long Id { get; set; }
    public long LotId { get; set; }
    public Lot? Lot { get; set; }
    public decimal BidAmount { get; set; }
    public DateTime BidTimeUtc { get; set; }
    public Source Source { get; set; }
}
