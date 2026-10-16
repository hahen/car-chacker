using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarPrices.Enrichment.Vpic;

/// <summary>Calls the free NHTSA vPIC DecodeVINValuesBatch endpoint (no API key required).</summary>
public class VpicClient(HttpClient http, IOptions<VpicOptions> options, ILogger<VpicClient> logger)
{
    public async Task<IReadOnlyList<VpicResult>> DecodeBatchAsync(
        IEnumerable<(string Vin, int? Year)> items, CancellationToken ct = default)
    {
        var list = items.Where(i => !string.IsNullOrWhiteSpace(i.Vin)).ToList();
        if (list.Count == 0) return Array.Empty<VpicResult>();

        // Batch format: "VIN[,year]" entries separated by ';'.
        var data = string.Join(";", list.Select(x => x.Year is int y ? $"{x.Vin},{y}" : x.Vin));
        var url = $"{options.Value.BaseUrl.TrimEnd('/')}/vehicles/DecodeVINValuesBatch/";
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["data"] = data,
            ["format"] = "json",
        });

        logger.LogInformation("vPIC decoding {Count} VIN(s)", list.Count);
        using var resp = await http.PostAsync(url, form, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        return VpicResponseParser.Parse(json);
    }

    public async Task<VpicResult?> DecodeAsync(string vinOrPattern, int? year, CancellationToken ct = default)
    {
        var results = await DecodeBatchAsync(new[] { (vinOrPattern, year) }, ct);
        return results.FirstOrDefault();
    }
}
