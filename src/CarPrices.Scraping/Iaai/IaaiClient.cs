using CarPrices.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CarPrices.Scraping.Iaai;

/// <summary>
/// Drives Chromium (Playwright) to fetch IAAI search-results HTML, which is server-rendered behind
/// Imperva/Incapsula. Requires browsers installed via `playwright install chromium`.
/// </summary>
public sealed class IaaiClient(IaaiOptions options, ILogger logger) : IAsyncDisposable
{
    private IPlaywright? _pw;
    private IBrowserContext? _ctx;
    private IPage? _page;

    public async Task StartAsync(CancellationToken ct = default)
    {
        _pw = await Microsoft.Playwright.Playwright.CreateAsync();
        var launch = new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = options.Headless,
            UserAgent = options.UserAgent,
            ViewportSize = new ViewportSize { Width = 1366, Height = 900 },
        };
        if (!string.IsNullOrWhiteSpace(options.ProxyServer))
            launch.Proxy = new Proxy { Server = options.ProxyServer! };

        var dir = options.UserDataDir ?? Path.Combine(CarDbPaths.DataDirectory(), ".playwright", "iaai");
        Directory.CreateDirectory(dir);

        _ctx = await _pw.Chromium.LaunchPersistentContextAsync(dir, launch);
        _page = _ctx.Pages.FirstOrDefault() ?? await _ctx.NewPageAsync();
        _page.SetDefaultTimeout(options.NavTimeoutMs);

        logger.LogInformation("Warming up IAAI session…");
        await _page.GotoAsync(options.BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await JitterAsync(ct);
    }

    public async Task<List<ScrapedLot>> SearchAsync(string keyword, CancellationToken ct = default, int? maxPages = null)
    {
        EnsureStarted();
        var all = new List<ScrapedLot>();
        var seen = new HashSet<string>();
        var pages = Math.Min(maxPages ?? options.MaxPages, options.MaxPages);

        for (var p = 1; p <= pages; p++)
        {
            ct.ThrowIfCancellationRequested();
            await _page!.GotoAsync(BuildSearchUrl(keyword, p), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await JitterAsync(ct);

            var html = await _page.ContentAsync();
            var lots = IaaiLotParser.ParseSearchHtml(html);
            var fresh = lots.Where(l => seen.Add(l.LotNumber)).ToList();
            if (fresh.Count == 0) break; // no new results -> stop paginating
            all.AddRange(fresh);
            logger.LogInformation("IAAI '{Kw}' page {Page}: {Count} lots ({Fresh} new)", keyword, p, lots.Count, fresh.Count);
        }
        return all;
    }

    /// <summary>Body text of a lot's detail page (best-effort, used by close-capture).</summary>
    public async Task<string?> GetDetailTextAsync(string lotUrl, CancellationToken ct = default)
    {
        EnsureStarted();
        try
        {
            await _page!.GotoAsync(lotUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await JitterAsync(ct);
            return await _page.InnerTextAsync("body");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "IAAI detail fetch failed for {Url}", lotUrl);
            return null;
        }
    }

    private string BuildSearchUrl(string keyword, int page)
    {
        var url = $"{options.BaseUrl}/Search?url=cars&Keyword={Uri.EscapeDataString(keyword)}";
        return page > 1 ? url + $"&page={page}" : url;
    }

    private Task JitterAsync(CancellationToken ct) =>
        Task.Delay(Random.Shared.Next(options.MinDelayMs, options.MaxDelayMs), ct);

    private void EnsureStarted()
    {
        if (_page is null) throw new InvalidOperationException("IaaiClient.StartAsync must be called first.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_ctx is not null) await _ctx.DisposeAsync();
        _pw?.Dispose();
    }
}
