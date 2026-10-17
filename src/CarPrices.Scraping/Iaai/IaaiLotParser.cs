using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace CarPrices.Scraping.Iaai;

/// <summary>
/// Parses IAAI search-results HTML into <see cref="ScrapedLot"/>s. Identity fields (VIN, year,
/// make/model/trim, item id, branch, auction date, status) come from the reliable onclick/href
/// attributes; body fields (odometer, damage, prices, engine) are parsed best-effort from the
/// card's data-list text and should be verified against live pages.
/// </summary>
public static partial class IaaiLotParser
{
    [GeneratedRegex(@"ImageModalClicked\(\s*'([^']*)'\s*,\s*'([^']*)'\s*,\s*'([^']*)'\s*,\s*'([^']*)'\s*,\s*'([^']*)'\s*,\s*'([^']*)'\s*,\s*'([^']*)'\s*,\s*'([^']*)'\s*,\s*'([^']*)'\s*\)")]
    private static partial Regex ImageModalRe();

    [GeneratedRegex(@"AddDelWatch\(\s*this\s*,\s*'([^']*)'\s*,\s*'([^']*)'\s*,\s*'([^']*)'\s*,\s*'([^']*)'\s*,\s*'([^']*)'\s*\)")]
    private static partial Regex WatchRe();

    [GeneratedRegex(@"([\d,]+)\s*mi\b", RegexOptions.IgnoreCase)]
    private static partial Regex OdometerRe();

    [GeneratedRegex(@"\$\s*([\d,]+(?:\.\d{2})?)")]
    private static partial Regex MoneyRe();

    [GeneratedRegex(@"^([A-Za-z][A-Za-z .'\-]+?)\s*\(([A-Za-z][A-Za-z .]+)\)$")]
    private static partial Regex LocationFullRe();

    public static IReadOnlyList<ScrapedLot> ParseSearchHtml(string html)
    {
        var parser = new HtmlParser();
        using var doc = parser.ParseDocument(html);
        var lots = new List<ScrapedLot>();
        foreach (var card in doc.QuerySelectorAll(".table-row-inner"))
        {
            var modal = card.QuerySelector("[onclick^=\"ImageModalClicked\"]");
            if (modal is null) continue; // header / non-data rows
            var lot = ParseCard(card, modal);
            if (lot is not null) lots.Add(lot);
        }
        return lots;
    }

    private static ScrapedLot? ParseCard(IElement card, IElement modal)
    {
        var m = ImageModalRe().Match(modal.GetAttribute("onclick") ?? "");
        if (!m.Success) return null;

        var stockNo = m.Groups[1].Value;
        var itemId = m.Groups[2].Value; // globally-unique, e.g. 46700483~US
        var lot = new ScrapedLot
        {
            LotNumber = string.IsNullOrWhiteSpace(itemId) ? stockNo : itemId,
            Vin = NullIfEmpty(m.Groups[3].Value),
            YardName = NullIfEmpty(m.Groups[4].Value),
            Year = ParseInt(m.Groups[5].Value),
            Make = NullIfEmpty(m.Groups[6].Value),
            Model = NullIfEmpty(m.Groups[7].Value),
            Trim = NullIfEmpty(m.Groups[8].Value),
            Currency = "USD",
        };

        var watch = WatchRe().Match(card.QuerySelector("[onclick*=\"AddDelWatch\"]")?.GetAttribute("onclick") ?? "");
        if (watch.Success)
        {
            lot.AuctionDateUtc = ParseDate(watch.Groups[3].Value);
            lot.LotStatus = NullIfEmpty(watch.Groups[5].Value);
        }

        var href = card.QuerySelector("a[href*=\"VehicleDetail\"]")?.GetAttribute("href");
        if (!string.IsNullOrWhiteSpace(href))
            lot.LotUrl = href!.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? href : "https://www.iaai.com" + href;

        var img = card.QuerySelector("img[data-src]")?.GetAttribute("data-src");
        if (!string.IsNullOrWhiteSpace(img)) lot.ImageUrls.Add(img!);

        // Title doc + primary/loss from the first search data-list section (ordered values).
        var section = card.QuerySelector(".data-list--search");
        if (section is not null)
        {
            var vals = section.QuerySelectorAll(".data-list__value")
                .Select(v => Collapse(v.TextContent)).Where(s => s.Length > 0).ToList();
            if (vals.Count > 0) lot.TitleDescription = vals[0];   // e.g. Salvage
            if (vals.Count > 1) lot.PrimaryDamage = vals[1];      // e.g. Front End
            if (vals.Count > 2) lot.SecondaryDamage = vals[2];    // e.g. Collision (loss type)
        }

        var text = Collapse(card.TextContent);

        var od = OdometerRe().Match(text);
        if (od.Success && int.TryParse(od.Groups[1].Value.Replace(",", ""), out var miles)) lot.Odometer = miles;

        if (text.Contains("Key Available", StringComparison.OrdinalIgnoreCase)) lot.HasKeys = true;
        else if (text.Contains("No Key", StringComparison.OrdinalIgnoreCase) ||
                 text.Contains("Key Not Available", StringComparison.OrdinalIgnoreCase)) lot.HasKeys = false;

        lot.RunAndDriveRaw = ExtractRunAndDrive(text);
        lot.FuelType = ExtractFuel(text);
        lot.EngineDescription = ExtractEngine(card);

        var amounts = MoneyRe().Matches(text)
            .Select(mm => decimal.TryParse(mm.Groups[1].Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : (decimal?)null)
            .Where(d => d.HasValue).Select(d => d!.Value).ToList();
        if (amounts.Count > 0) lot.CurrentBid = amounts[0];
        if (amounts.Count > 1) lot.BuyNowPrice = amounts[^1];

        ParseLocation(card, lot);

        lot.RawJson = card.OuterHtml.Length > 8000 ? card.OuterHtml[..8000] : card.OuterHtml;
        return lot;
    }

    private static string? ExtractRunAndDrive(string text)
    {
        if (text.Contains("Run & Drive", StringComparison.OrdinalIgnoreCase)) return "Run & Drive";
        if (text.Contains("Engine Start", StringComparison.OrdinalIgnoreCase)) return "Engine Start Program";
        if (text.Contains("Starts", StringComparison.OrdinalIgnoreCase)) return "Starts";
        if (text.Contains("Stationary", StringComparison.OrdinalIgnoreCase)) return "Stationary";
        return null;
    }

    private static string? ExtractFuel(string text)
    {
        foreach (var f in new[] { "Gasoline", "Diesel", "Electric", "Hybrid", "Flex" })
            if (text.Contains(f, StringComparison.OrdinalIgnoreCase)) return f;
        return null;
    }

    private static string? ExtractEngine(IElement card)
    {
        foreach (var v in card.QuerySelectorAll(".data-list__value"))
        {
            var t = Collapse(v.TextContent);
            if (Regex.IsMatch(t, @"\d(\.\d)?L", RegexOptions.IgnoreCase) && t.Length < 80)
                return t;
        }
        return null;
    }

    /// <summary>Finds a leaf element whose whole text is "City (State)" to avoid cross-element bleed.</summary>
    private static void ParseLocation(IElement card, ScrapedLot lot)
    {
        foreach (var el in card.QuerySelectorAll("*"))
        {
            if (el.ChildElementCount > 0) continue;
            var m = LocationFullRe().Match(Collapse(el.TextContent));
            if (!m.Success) continue;
            lot.LocationCity = m.Groups[1].Value.Trim();
            lot.LocationState = m.Groups[2].Value.Trim();
            lot.LocationCountry = "USA";
            return;
        }
    }

    private static DateTime? ParseDate(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto)
            ? dto.UtcDateTime : null;
    }

    private static int? ParseInt(string s) => int.TryParse(s, out var v) ? v : null;
    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string Collapse(string? s) => string.IsNullOrEmpty(s) ? string.Empty : Regex.Replace(s, @"\s+", " ").Trim();
}
