using System.Text.Json;

namespace CarPrices.Scraping.Copart;

/// <summary>
/// Parses a Copart <c>/public/lots/search-results</c> response into <see cref="ScrapedLot"/>s.
/// Field keys were captured by inspecting live responses; unknown keys are ignored and the raw
/// JSON is retained per lot so mappings can be refined without re-scraping.
/// </summary>
public static class CopartLotParser
{
    public static IReadOnlyList<ScrapedLot> ParseResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var content = FindResultsArray(doc.RootElement);
        var lots = new List<ScrapedLot>();
        if (content is null) return lots;
        foreach (var el in content.Value.EnumerateArray())
        {
            if (el.ValueKind == JsonValueKind.Object)
                lots.Add(ParseLot(el));
        }
        return lots;
    }

    /// <summary>data.results.content[] (or data.results[] / results[]), wherever the array lives.</summary>
    private static JsonElement? FindResultsArray(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array) return root;
        if (root.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("results", out var results))
            {
                if (results.ValueKind == JsonValueKind.Array) return results;
                if (results.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.Array) return c;
            }
        }
        if (root.TryGetProperty("results", out var r) && r.ValueKind == JsonValueKind.Array) return r;
        return null;
    }

    public static ScrapedLot ParseLot(JsonElement el)
    {
        var lot = new ScrapedLot
        {
            LotNumber = Str(el, "lotNumberStr") ?? Long(el, "ln")?.ToString() ?? string.Empty,
            Vin = Str(el, "fv"),
            Make = Str(el, "mkn"),
            Model = Str(el, "lmg") ?? Str(el, "lm"),
            Trim = Str(el, "ltd"),
            Year = Int(el, "lcy"),
            CurrentBid = Dec(el, "hb"),
            BuyNowPrice = Positive(Dec(el, "bnp")),
            EstimatedRetailValue = Positive(Dec(el, "lotPlugAcv")),
            Currency = Str(el, "cuc"),
            AuctionDateUtc = Epoch(el, "ad"),
            LastAuctionDateUtc = Epoch(el, "lad"),
            SaleStatusCode = Int(el, "ss"),
            LotStatus = Str(el, "lsts"),
            YardName = Str(el, "yn"),
            LocationCity = Str(el, "locCity"),
            LocationState = Str(el, "locState"),
            LocationCountry = Str(el, "locCountry"),
            Odometer = Int(el, "orr"),
            OdometerBrandCode = Str(el, "obc"),
            Color = Str(el, "clr"),
            DriveType = Str(el, "drv"),
            FuelType = Str(el, "ft"),
            EngineDescription = Str(el, "egn"),
            TitleDescription = Str(el, "td"),
            TitleGroupCode = Str(el, "tgc"),
            PrimaryDamage = Str(el, "dd"),
            SecondaryDamage = Str(el, "sdd"),
            HasKeys = Bool(el, "hk"),
            RawJson = el.GetRawText(),
        };
        return lot;
    }

    private static string? Str(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var v)) return null;
        var s = v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number => v.ToString(),
            _ => null,
        };
        return string.IsNullOrWhiteSpace(s) ? null : s!.Trim();
    }

    private static int? Int(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
        if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var j)) return j;
        return null;
    }

    private static long? Long(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var i)) return i;
        if (v.ValueKind == JsonValueKind.String && long.TryParse(v.GetString(), out var j)) return j;
        return null;
    }

    private static decimal? Dec(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d)) return d;
        if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(), out var e)) return e;
        return null;
    }

    private static decimal? Positive(decimal? v) => v is > 0 ? v : null;

    private static bool? Bool(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => v.GetString()?.Trim().ToUpperInvariant() is "YES" or "TRUE" or "Y",
            _ => null,
        };
    }

    private static DateTime? Epoch(JsonElement el, string key)
    {
        var ms = Long(el, key);
        if (ms is null or <= 0) return null;
        return DateTimeOffset.FromUnixTimeMilliseconds(ms.Value).UtcDateTime;
    }
}
