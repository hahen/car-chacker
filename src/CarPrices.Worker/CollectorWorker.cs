using CarPrices.Infrastructure;
using CarPrices.Scraping.Copart;
using CarPrices.Scraping.Iaai;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CarPrices.Worker;

public class WorkerOptions
{
    /// <summary>Master switch — disabled by default so the scheduler never runs Playwright unexpectedly.</summary>
    public bool Enabled { get; set; } = false;
    public int DiscoveryIntervalMinutes { get; set; } = 360;   // every 6h
    public int CloseCaptureIntervalMinutes { get; set; } = 60; // hourly
    public bool RunOnStartup { get; set; } = false;
}

/// <summary>Runs Copart discovery and close-capture on intervals (when enabled in config).</summary>
public class CollectorWorker(
    IServiceProvider services,
    IOptions<WorkerOptions> options,
    ILogger<CollectorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using (var scope = services.CreateScope())
            DbInitializer.Initialize(scope.ServiceProvider.GetRequiredService<AppDbContext>());

        var o = options.Value;
        if (!o.Enabled)
        {
            logger.LogInformation("Collector worker is disabled. Set Worker:Enabled=true to schedule runs.");
            return;
        }

        var discoveryEvery = TimeSpan.FromMinutes(Math.Max(1, o.DiscoveryIntervalMinutes));
        var closeEvery = TimeSpan.FromMinutes(Math.Max(1, o.CloseCaptureIntervalMinutes));
        var lastDiscovery = o.RunOnStartup ? DateTime.MinValue : DateTime.UtcNow;
        var lastClose = o.RunOnStartup ? DateTime.MinValue : DateTime.UtcNow;

        logger.LogInformation("Collector worker started (discovery every {D}m, close-capture every {C}m).",
            o.DiscoveryIntervalMinutes, o.CloseCaptureIntervalMinutes);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            var now = DateTime.UtcNow;
            try
            {
                if (now - lastDiscovery >= discoveryEvery) { await RunDiscoveryAsync(ct); lastDiscovery = now; }
                if (now - lastClose >= closeEvery) { await RunCloseAsync(ct); lastClose = now; }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { logger.LogError(ex, "Worker cycle failed"); }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task RunDiscoveryAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var copart = await scope.ServiceProvider.GetRequiredService<CopartCollector>().RunDiscoveryAsync(ct);
        logger.LogInformation("Copart discovery #{Id}: new={New} updated={Upd} errors={Err}", copart.RunId, copart.New, copart.Updated, copart.Errors);
        var iaai = await scope.ServiceProvider.GetRequiredService<IaaiCollector>().RunDiscoveryAsync(ct);
        logger.LogInformation("IAAI discovery #{Id}: new={New} updated={Upd} errors={Err}", iaai.RunId, iaai.New, iaai.Updated, iaai.Errors);
    }

    private async Task RunCloseAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var copart = await scope.ServiceProvider.GetRequiredService<CopartCollector>().RunCloseCaptureAsync(200, ct);
        logger.LogInformation("Copart close-capture #{Id}: captured={Cap} errors={Err}", copart.RunId, copart.PricesCaptured, copart.Errors);
        var iaai = await scope.ServiceProvider.GetRequiredService<IaaiCollector>().RunCloseCaptureAsync(100, ct);
        logger.LogInformation("IAAI close-capture #{Id}: captured={Cap} errors={Err}", iaai.RunId, iaai.PricesCaptured, iaai.Errors);
    }
}
