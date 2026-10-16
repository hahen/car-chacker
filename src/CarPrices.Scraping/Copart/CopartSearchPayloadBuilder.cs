using System.Text.Json;

namespace CarPrices.Scraping.Copart;

/// <summary>Builds the JSON body for Copart's <c>/public/lots/search-results</c> endpoint.</summary>
public static class CopartSearchPayloadBuilder
{
    public static string BuildSearchPayload(string? keyword, int page, int size)
    {
        var freeForm = !string.IsNullOrWhiteSpace(keyword);
        var payload = new Dictionary<string, object?>
        {
            ["query"] = new[] { freeForm ? keyword! : "*" },
            ["filter"] = new Dictionary<string, object?>(),
            ["sort"] = new[] { "auction_date_type asc" },
            ["page"] = page,
            ["size"] = size,
            ["start"] = page * size,
            ["watchListOnly"] = false,
            ["freeFormSearch"] = freeForm,
            ["hideImages"] = true,
            ["defaultSort"] = !freeForm,
            ["specificRowProvided"] = false,
            ["displayName"] = keyword ?? string.Empty,
            ["searchName"] = keyword ?? string.Empty,
            ["backUrl"] = string.Empty,
        };
        return JsonSerializer.Serialize(payload);
    }

    /// <summary>Free-form keyword from a target's make/model/trim, e.g. "BMW 3 Series".</summary>
    public static string BuildKeyword(string? make, string? model, string? trim)
    {
        var parts = new[] { make, model, trim }.Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(' ', parts).Trim();
    }
}
