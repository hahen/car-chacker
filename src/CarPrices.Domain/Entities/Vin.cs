using CarPrices.Domain.Enums;

namespace CarPrices.Domain.Entities;

/// <summary>Cached VIN decode result (by full VIN or 11-char pattern).</summary>
public class VinDecode
{
    public long Id { get; set; }
    public string VinOrPattern { get; set; } = string.Empty;
    public string Source { get; set; } = "vPIC";
    public DateTime DecodedAtUtc { get; set; } = DateTime.UtcNow;
    public string? RawJson { get; set; }

    public string? MakerName { get; set; }
    public string? ModelName { get; set; }
    public string? TrimName { get; set; }
    public int? ModelYear { get; set; }
    public string? BodyClass { get; set; }
    public string? DriveType { get; set; }
    public string? FuelType { get; set; }
    public string? EngineModel { get; set; }
    public string? PlantCountry { get; set; }
}

/// <summary>Audit record of one attempt to recover a VIN from a lot image.</summary>
public class VinExtraction
{
    public long Id { get; set; }
    public long LotId { get; set; }
    public Lot? Lot { get; set; }
    public long? LotImageId { get; set; }
    public LotImage? LotImage { get; set; }
    public VinExtractionMethod Method { get; set; }
    public string? Engine { get; set; }
    public string? RawText { get; set; }
    public string? CandidateVin { get; set; }
    public bool CheckDigitValid { get; set; }
    public bool MatchesMaskedPattern { get; set; }
    public double Confidence { get; set; }
    public bool Accepted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
