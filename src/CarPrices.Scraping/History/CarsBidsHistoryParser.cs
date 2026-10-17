using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace CarPrices.Scraping.History;

/// <summary>Identity of a car detail page (parsed from its URL).</summary>
public record CbhIdentity(int? Year, string? Make, string? Model, string? Trim, string? Vin, string DetailUrl);

/// <summary>
/// Parses carsbidshistory.com: the makes list (make id), a make page (model ids), a model listing
/// (car detail URLs), and a car detail page (per-VIN bid-history tables → sold ScrapedLots).
/// </summary>
public static partial class CarsBidsHistoryParser
{
    [GeneratedRegex(@"/make/(\d+)-([A-Z0-9_]+)(?=[""'/\s])")]
    private static partial Regex MakeLinkRe();

    [GeneratedRegex(@"/make/\d+-[A-Z0-9_]+/(\d+)-([A-Z0-9_]+)(?=[""'/\s])")]
    private static partial Regex ModelLinkRe();

    [GeneratedRegex(@"/make/\d+-[A-Z0-9_]+/\d+-[A-Z0-9_]+/\d{4}_[A-Z0-9_]+_\d+_[A-HJ-NPR-Z0-9]{6,17}")]
    private static partial Regex DetailUrlRe();

    [GeneratedRegex(@"page-(\d+)")]
    private static partial Regex PageRe();

    /// <summary>Finds a make's numeric id in a makes-list page by its URL name (e.g. "BMW").</summary>
    public static string? FindMakeId(string html, string makeUrlName)
    {
        foreach (Match m in MakeLinkRe().Matches(html))
            if (string.Equals(m.Groups[2].Value, makeUrlName, StringComparison.OrdinalIgnoreCase))
                return m.Groups[1].Value;
        return null;
    }

    /// <summary>Models of a make whose name contains all query tokens (e.g. ["3","SERIES"] or ["330I"]).</summary>
    public static List<(string Id, string Name)> FindModels(string html, string makeId, string[] tokens)
    {
        var seen = new HashSet<string>();
        var result = new List<(string, string)>();
        foreach (Match m in ModelLinkRe().Matches(html))
        {
            // ModelLinkRe matches any make id; keep only this make's rows.
            if (!m.Value.StartsWith($"/make/{makeId}-", StringComparison.Ordinal)) continue;
            var id = m.Groups[1].Value;
            var name = m.Groups[2].Value;
            if (!seen.Add(id)) continue;
            var upper = name.ToUpperInvariant();
            if (tokens.All(t => upper.Contains(t.ToUpperInvariant())))
                result.Add((id, name));
        }
        return result;
    }

    public static List<string> ParseListingDetailUrls(string html) =>
        DetailUrlRe().Matches(html).Select(m => m.Value).Distinct().ToList();

    public static int ParseMaxListingPage(string html)
    {
        var max = 1;
        foreach (Match m in PageRe().Matches(html))
            if (int.TryParse(m.Groups[1].Value, out var n) && n > max) max = n;
        return max;
    }

    /// <summary>Parses year/make/model/VIN out of a detail URL path.</summary>
    public static CbhIdentity ParseIdentity(string detailUrl)
    {
        // /make/<id>-<MAKE>/<id>-<MODEL>/<year>_<MAKE>_<MODEL>_<lot>_<VIN>
        var segs = detailUrl.TrimEnd('/').Split('/');
        string? make = null, model = null;
        if (segs.Length >= 5)
        {
            make = Denorm(StripId(segs[^3]));
            model = Denorm(StripId(segs[^2]));
        }
        var last = segs[^1];
        int? year = int.TryParse(last.Length >= 4 ? last[..4] : "", out var y) ? y : null;
        var vinMatch = Regex.Match(last, @"_([A-HJ-NPR-Z0-9]{11,17})$");
        var vin = vinMatch.Success ? vinMatch.Groups[1].Value : null;
        return new CbhIdentity(year, make, model, null, vin, detailUrl);
    }

    /// <summary>Parses a detail page into one sold ScrapedLot per sold bid-history row.</summary>
    public static List<ScrapedLot> ParseDetail(string html, CbhIdentity id)
    {
        var parser = new HtmlParser();
        using var doc = parser.ParseDocument(html);

        var details = doc.QuerySelectorAll(".table-details").ToList();
        var auctions = doc.QuerySelectorAll(".auction-table").ToList();
        var lotNumbers = LotNumbers(html);

        var lots = new List<ScrapedLot>();
        var blocks = Math.Min(details.Count, auctions.Count);
        for (var i = 0; i < blocks; i++)
        {
            var kv = DetailsKeyValues(details[i]);
            var lotNo = i < lotNumbers.Count ? lotNumbers[i] : (id.Vin ?? $"cbh{i}");
            var images = doc.QuerySelectorAll($"img")
                .Select(im => im.GetAttribute("src") ?? im.GetAttribute("data-src"))
                .Where(s => !string.IsNullOrWhiteSpace(s) && s!.Contains($"/photo2/{lotNo}/"))
                .Distinct().Take(6).ToList();

            // A lot's auction table holds its bid history over time; keep the single most recent sale.
            (DateTime? Date, decimal Bid, string Status, string? Loc)? best = null;
            foreach (var row in auctions[i].QuerySelectorAll("tr"))
            {
                var cells = row.QuerySelectorAll("td").Select(c => Collapse(c.TextContent)).ToList();
                if (cells.Count < 7) continue; // header or malformed
                var bid = ParseDecimal(cells[5]);
                if (bid is not > 0 || !cells[6].Contains("Sold", StringComparison.OrdinalIgnoreCase)) continue;
                var date = ParseDate(cells[1]);
                if (best is null || (date is not null && (best.Value.Date is null || date > best.Value.Date)))
                    best = (date, bid.Value, cells[6], cells.Count > 7 ? NullIfEmpty(cells[7]) : null);
            }
            if (best is null) continue;

            var lot = new ScrapedLot
            {
                LotNumber = lotNo,
                Vin = id.Vin,
                Make = id.Make,
                Model = id.Model,
                Trim = id.Trim,
                Year = id.Year,
                AuctionDateUtc = best.Value.Date,
                SalePrice = best.Value.Bid,
                IsSold = true,
                CurrentBid = best.Value.Bid,
                Currency = "USD",
                LotStatus = best.Value.Status,
                LotUrl = id.DetailUrl,
                Odometer = ParseInt(Get(kv, "Odometer")),
                Color = Get(kv, "Color"),
                DriveType = Get(kv, "Drive"),
                FuelType = Get(kv, "Fuel"),
                EngineDescription = Get(kv, "Engine"),
                TitleDescription = Get(kv, "Doc Type"),
                PrimaryDamage = Get(kv, "Primary Damage"),
                SecondaryDamage = Get(kv, "Secondary Damage"),
                EstimatedRetailValue = ParseDecimal(Get(kv, "Estimate Retail Value")),
                LocationState = best.Value.Loc,
                RawJson = $"{{\"status\":\"{best.Value.Status}\"}}",
            };
            lot.ImageUrls.AddRange(images);
            lots.Add(lot);
        }
        return lots;
    }

    private static List<string> LotNumbers(string html) =>
        Regex.Matches(html, @"\bLot:\s*(\d+)").Select(m => m.Groups[1].Value).ToList();

    private static Dictionary<string, string> DetailsKeyValues(IElement table)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tr in table.QuerySelectorAll("tr"))
        {
            var cells = tr.QuerySelectorAll("td,th").ToList();
            if (cells.Count < 2) continue;
            var key = Collapse(cells[0].TextContent).TrimEnd(':').Trim();
            var val = Collapse(cells[1].TextContent);
            if (key.Length > 0 && !dict.ContainsKey(key)) dict[key] = val;
        }
        return dict;
    }

    private static string? Get(Dictionary<string, string> kv, string key) =>
        kv.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;

    private static string StripId(string seg)
    {
        var dash = seg.IndexOf('-');
        return dash >= 0 ? seg[(dash + 1)..] : seg;
    }

    private static string Denorm(string s) => s.Replace('_', ' ').Trim();

    private static int? ParseInt(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var m = Regex.Match(s, @"\d[\d,]*");
        return m.Success && int.TryParse(m.Value.Replace(",", ""), out var v) ? v : null;
    }

    private static decimal? ParseDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var cleaned = s.Replace("$", "").Replace(",", "").Trim();
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static DateTime? ParseDate(string s) =>
        DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d)
            ? d : null;

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string Collapse(string? s) => string.IsNullOrEmpty(s) ? string.Empty : Regex.Replace(s, @"\s+", " ").Trim();
}
