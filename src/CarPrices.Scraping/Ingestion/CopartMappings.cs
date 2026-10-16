using CarPrices.Domain.Enums;

namespace CarPrices.Scraping.Ingestion;

/// <summary>Pure mapping helpers from Copart raw values to normalized enums/codes (unit-testable).</summary>
public static class CopartMappings
{
    public static OdometerBrand OdometerBrandFrom(string? obc) =>
        obc?.Trim().ToUpperInvariant() switch
        {
            "A" => OdometerBrand.Actual,
            "N" => OdometerBrand.NotActual,
            "E" => OdometerBrand.Exempt,
            "X" => OdometerBrand.ExceedsMechanicalLimit,
            _ => OdometerBrand.Unknown,
        };

    public static TitleGroup TitleGroupFrom(string? tgc, string? titleDesc)
    {
        var c = tgc?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(c))
        {
            if (c.EndsWith("_S") || c.Contains("SALV")) return TitleGroup.Salvage;
            if (c.EndsWith("_C") || c.Contains("CLEAN")) return TitleGroup.Clean;
            if (c.EndsWith("_R") || c.Contains("REBUIL")) return TitleGroup.Rebuilt;
            if (c.EndsWith("_J") || c.Contains("JUNK")) return TitleGroup.Junk;
            if (c.EndsWith("_N") || c.Contains("NONREP")) return TitleGroup.Nonrepairable;
        }
        var t = titleDesc?.Trim().ToUpperInvariant() ?? string.Empty;
        if (t.Contains("NONREPAIR")) return TitleGroup.Nonrepairable;
        if (t.Contains("REBUILT") || t.Contains("RECONSTRUCT")) return TitleGroup.Rebuilt;
        if (t.Contains("JUNK") || t.Contains("DESTRUCT")) return TitleGroup.Junk;
        if (t.Contains("SALVAGE") || t.Contains("FLOOD") || t.Contains("WATER")) return TitleGroup.Salvage;
        if (t.Contains("CLEAN") || t.Contains("CLEAR")) return TitleGroup.Clean;
        return TitleGroup.Unknown;
    }

    /// <summary>Representative seeded TitleType code for a title group (null = unknown/none).</summary>
    public static string? TitleTypeCodeFor(TitleGroup group) => group switch
    {
        TitleGroup.Clean => "CLEAN",
        TitleGroup.Salvage => "SALVAGE",
        TitleGroup.Rebuilt => "REBUILT",
        TitleGroup.Junk => "JUNK",
        TitleGroup.Nonrepairable => "NONREPAIRABLE",
        _ => null,
    };

    /// <summary>Maps a Copart damage description to a seeded DamageType code (null = none).</summary>
    public static string? DamageCodeFrom(string? text)
    {
        var t = text?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(t)) return null;
        if (t.Contains("FRONT")) return "FRONT";
        if (t.Contains("REAR")) return "REAR";
        if (t.Contains("SIDE")) return "SIDE";
        if (t.Contains("ALL OVER")) return "ALL_OVER";
        if (t.Contains("WATER") || t.Contains("FLOOD")) return "WATER";
        if (t.Contains("BURN") || t.Contains("FIRE")) return "FIRE";
        if (t.Contains("HAIL")) return "HAIL";
        if (t.Contains("VANDAL")) return "VANDALISM";
        if (t.Contains("MECHAN")) return "MECHANICAL";
        if (t.Contains("NORMAL WEAR") || t.Contains("NORMAL")) return "NORMAL_WEAR";
        if (t.Contains("UNDERCARRIAGE")) return "UNDERCARRIAGE";
        if (t.Contains("ROLLOVER")) return "ROLLOVER";
        if (t.Contains("MINOR") || t.Contains("SCRATCH") || t.Contains("DENT")) return "MINOR_DENT";
        return "UNKNOWN";
    }

    /// <summary>Discovery-time sale status: upcoming if the auction is in the future, else unknown
    /// (final status is resolved by the close-capture pass).</summary>
    public static SaleStatus DiscoverySaleStatus(DateTime? auctionDateUtc, DateTime nowUtc) =>
        auctionDateUtc is null ? SaleStatus.Unknown
        : auctionDateUtc > nowUtc ? SaleStatus.Upcoming
        : SaleStatus.Unknown;

    /// <summary>Cleans a raw VIN and classifies it. Returns (fullVin, pattern): fullVin is set only for
    /// a complete unmasked 17-char VIN; pattern is the first 11 chars when available.</summary>
    public static (string? FullVin, string? Pattern) ClassifyVin(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (null, null);
        var v = new string(raw.Trim().ToUpperInvariant().Where(c => !char.IsWhiteSpace(c)).ToArray());
        string? pattern = v.Length >= 11 && !v[..11].Contains('*') ? v[..11] : null;
        var full = v.Length == 17 && !v.Contains('*') ? v : null;
        return (full, pattern);
    }
}
