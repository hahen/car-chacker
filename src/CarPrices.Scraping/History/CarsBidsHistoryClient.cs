using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarPrices.Scraping.History;

/// <summary>Thin HTTP client for carsbidshistory.com (server-rendered HTML, no browser needed).</summary>
public class CarsBidsHistoryClient(HttpClient http, IOptions<HistoryOptions> options, ILogger<CarsBidsHistoryClient> logger)
{
    public HistoryOptions Options => options.Value;

    public async Task<string?> GetHtmlAsync(string pathOrUrl, CancellationToken ct = default)
    {
        var url = pathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? pathOrUrl
            : options.Value.BaseUrl.TrimEnd('/') + pathOrUrl;
        try
        {
            using var resp = await http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("carsbidshistory {Url} -> HTTP {Status}", url, (int)resp.StatusCode);
                return null;
            }
            return await resp.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "carsbidshistory fetch failed: {Url}", url);
            return null;
        }
    }

    public Task JitterAsync(CancellationToken ct) =>
        Task.Delay(Random.Shared.Next(options.Value.MinDelayMs, options.Value.MaxDelayMs), ct);
}
