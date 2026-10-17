using System.Text.Json;
using CarPrices.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CarPrices.Scraping.Copart;

/// <summary>
/// Drives a real Chromium (via Playwright) to query Copart's public search endpoint.
/// The JSON fetch runs *inside* a warmed-up page so the Imperva/Incapsula cookies and JS apply —
/// plain HTTP clients get challenged. Requires browsers installed once via `playwright install chromium`.
/// </summary>
public sealed class CopartClient(CopartOptions options, ILogger logger) : IAsyncDisposable
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

        var dir = options.UserDataDir ?? Path.Combine(CarDbPaths.DataDirectory(), ".playwright", "copart");
        Directory.CreateDirectory(dir);

        _ctx = await _pw.Chromium.LaunchPersistentContextAsync(dir, launch);
        _page = _ctx.Pages.FirstOrDefault() ?? await _ctx.NewPageAsync();
        _page.SetDefaultTimeout(options.NavTimeoutMs);

        logger.LogInformation("Warming up Copart session…");
        try
        {
            // Commit returns as soon as the server responds; Copart's Incapsula-heavy homepage often
            // never fires DOMContentLoaded under the headless shell.
            await _page.GotoAsync(options.BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.Commit, Timeout = options.NavTimeoutMs });
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Copart warm-up navigation timed out; continuing (Incapsula may still set cookies).");
        }
        await Task.Delay(5000, ct); // let Incapsula's JS challenge run and set cookies
        await JitterAsync(ct);
    }

    /// <summary>Runs a free-form keyword search, paginating up to MaxPages (Copart caps at ~50).</summary>
    public async Task<List<ScrapedLot>> SearchAsync(string? keyword, CancellationToken ct = default, int? maxPages = null)
    {
        EnsureStarted();
        var all = new List<ScrapedLot>();
        var pages = Math.Min(maxPages ?? options.MaxPages, 50);
        for (var p = 0; p < pages; p++)
        {
            ct.ThrowIfCancellationRequested();
            var body = CopartSearchPayloadBuilder.BuildSearchPayload(keyword, p, options.PageSize);
            var json = await PostSearchAsync(body, ct);
            if (json is null) break;

            var lots = CopartLotParser.ParseResponse(json);
            if (lots.Count == 0) break;
            all.AddRange(lots);
            logger.LogInformation("Copart '{Keyword}' page {Page}: {Count} lots", keyword, p, lots.Count);
            if (lots.Count < options.PageSize) break; // last page
            await JitterAsync(ct);
        }
        return all;
    }

    /// <summary>Re-fetches a single lot by number (used by close-capture).</summary>
    public async Task<ScrapedLot?> GetLotByNumberAsync(string lotNumber, CancellationToken ct = default)
    {
        var lots = await SearchAsync(lotNumber, ct, maxPages: 1);
        return lots.FirstOrDefault(l => l.LotNumber == lotNumber) ?? lots.FirstOrDefault();
    }

    private async Task<string?> PostSearchAsync(string body, CancellationToken ct)
    {
        var url = options.BaseUrl + options.SearchPath;
        const string js = """
            async ([url, body]) => {
              const r = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                body
              });
              const text = await r.text();
              return JSON.stringify({ status: r.status, text });
            }
            """;
        var raw = await _page!.EvaluateAsync<string>(js, new object[] { url, body });
        using var doc = JsonDocument.Parse(raw);
        var status = doc.RootElement.GetProperty("status").GetInt32();
        if (status != 200)
        {
            logger.LogWarning("Copart search returned HTTP {Status} — backing off (possible challenge)", status);
            await Task.Delay(options.MaxDelayMs, ct);
            return null;
        }
        return doc.RootElement.GetProperty("text").GetString();
    }

    private Task JitterAsync(CancellationToken ct) =>
        Task.Delay(Random.Shared.Next(options.MinDelayMs, options.MaxDelayMs), ct);

    private void EnsureStarted()
    {
        if (_page is null) throw new InvalidOperationException("CopartClient.StartAsync must be called first.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_ctx is not null) await _ctx.DisposeAsync();
        _pw?.Dispose();
    }
}
