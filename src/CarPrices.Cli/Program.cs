using CarPrices.Domain.Entities;
using CarPrices.Domain.Vin;
using CarPrices.Enrichment;
using CarPrices.Infrastructure;
using CarPrices.Vision;
using CarPrices.Scraping;
using CarPrices.Scraping.Collection;
using CarPrices.Scraping.Copart;
using CarPrices.Scraping.Iaai;
using CarPrices.Scraping.VinCheck;
using CarPrices.Statistics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

const string DefaultVinCheckUrl = "https://vincheck.it.com/data/salvage-auction-statistics.csv";

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Default"));
builder.Services.AddStatistics();
builder.Services.AddScraping(builder.Configuration);
builder.Services.AddEnrichment(builder.Configuration);
builder.Services.AddVision();
using var host = builder.Build();

using var scope = host.Services.CreateScope();
var sp = scope.ServiceProvider;

var cmd = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
var sub = args.Length > 1 ? args[1].ToLowerInvariant() : "";

switch (cmd)
{
    case "db" when sub == "migrate":
        DbInitializer.Initialize(sp.GetRequiredService<AppDbContext>());
        Console.WriteLine("Database migrated + seeded.");
        break;

    case "import" when sub == "vincheck":
    {
        var importer = sp.GetRequiredService<VinCheckImporter>();
        VinCheckImporter.ImportResult r;
        if (HasFlag(args, "--sample"))
        {
            var path = Path.Combine(CarDbPaths.DataDirectory(), "sample", "vincheck-sample.csv");
            r = await importer.ImportFromFileAsync(path);
        }
        else if (TryGetOption(args, "--file", out var file))
        {
            r = await importer.ImportFromFileAsync(file);
        }
        else
        {
            var url = TryGetOption(args, "--url", out var u) ? u : DefaultVinCheckUrl;
            r = await importer.ImportFromUrlAsync(url);
        }
        Console.WriteLine($"VinCheck import: {r.Rows} rows ({r.Inserted} new, {r.Updated} updated).");
        break;
    }

    case "stats" when sub == "overview":
    {
        var stats = sp.GetRequiredService<StatisticsService>();
        var o = await stats.GetMarketOverviewAsync();
        Console.WriteLine($"Makes: {o.Makes}");
        Console.WriteLine($"Models: {o.Models}");
        Console.WriteLine($"Index rows: {o.IndexRows}");
        Console.WriteLine($"Total observations: {o.TotalObservations}");
        Console.WriteLine($"Median price: {(o.MedianPrice.HasValue ? o.MedianPrice.Value.ToString("C0") : "n/a")}");
        Console.WriteLine($"Year range: {o.MinYear}-{o.MaxYear}");
        break;
    }

    case "makes":
    {
        var stats = sp.GetRequiredService<StatisticsService>();
        foreach (var m in await stats.GetMakeSummariesAsync())
            Console.WriteLine($"{m.Make,-18} models={m.Models,-3} obs={m.Observations,-6} median={(m.MedianPrice.HasValue ? m.MedianPrice.Value.ToString("C0") : "n/a")}");
        break;
    }

    case "targets" when sub == "sync":
    {
        var svc = sp.GetRequiredService<CollectionTargetService>();
        Console.WriteLine($"Synced targets from config: {await svc.SyncFromConfigAsync()} added.");
        break;
    }

    case "targets" when sub == "list":
    {
        var svc = sp.GetRequiredService<CollectionTargetService>();
        foreach (var t in await svc.GetAllAsync())
            Console.WriteLine($"[{(t.Enabled ? "x" : " ")}] {t.MakeText} {t.ModelText} {t.TrimText} {t.YearFrom}-{t.YearTo} sources={t.Sources ?? "all"}");
        break;
    }

    case "targets" when sub == "add":
    {
        var make = TryGetOption(args, "--make", out var mk) ? mk : "";
        if (string.IsNullOrWhiteSpace(make)) { Console.WriteLine("--make is required."); break; }
        var cdb = sp.GetRequiredService<AppDbContext>();
        cdb.CollectionTargets.Add(new CollectionTarget
        {
            MakeText = make,
            ModelText = TryGetOption(args, "--model", out var mo) ? mo : null,
            TrimText = TryGetOption(args, "--trim", out var tr) ? tr : null,
            YearFrom = TryGetOption(args, "--year-from", out var yf) && int.TryParse(yf, out var yfi) ? yfi : null,
            YearTo = TryGetOption(args, "--year-to", out var yt) && int.TryParse(yt, out var yti) ? yti : null,
            Sources = TryGetOption(args, "--sources", out var sc) ? sc : null,
            Enabled = true,
        });
        await cdb.SaveChangesAsync();
        Console.WriteLine($"Added target: {make} {(TryGetOption(args, "--model", out var m2) ? m2 : "")}");
        break;
    }

    case "scrape" when sub == "copart":
    {
        var collector = sp.GetRequiredService<CopartCollector>();
        var r = await collector.RunDiscoveryAsync();
        Console.WriteLine($"Discovery run #{r.RunId}: seen={r.LotsSeen} new={r.New} updated={r.Updated} errors={r.Errors}");
        break;
    }

    case "scrape" when sub == "iaai":
    {
        var collector = sp.GetRequiredService<IaaiCollector>();
        var r = await collector.RunDiscoveryAsync();
        Console.WriteLine($"IAAI discovery run #{r.RunId}: seen={r.LotsSeen} new={r.New} updated={r.Updated} errors={r.Errors}");
        break;
    }

    case "capture-close" when sub == "iaai":
    {
        var collector = sp.GetRequiredService<IaaiCollector>();
        var r = await collector.RunCloseCaptureAsync();
        Console.WriteLine($"IAAI close-capture run #{r.RunId}: seen={r.LotsSeen} captured={r.PricesCaptured} updated={r.Updated} errors={r.Errors}");
        break;
    }

    case "capture-close":
    {
        var collector = sp.GetRequiredService<CopartCollector>();
        var r = await collector.RunCloseCaptureAsync();
        Console.WriteLine($"Close-capture run #{r.RunId}: seen={r.LotsSeen} captured={r.PricesCaptured} updated={r.Updated} errors={r.Errors}");
        break;
    }

    case "enrich":
    {
        var svc = sp.GetRequiredService<EnrichmentService>();
        var r = await svc.RunAsync();
        Console.WriteLine($"Enrichment: {r.CarsProcessed} cars, {r.Decoded} decoded, {r.FeaturesAdded} features, {r.Errors} errors");
        break;
    }

    case "vin-from-image":
    {
        if (!(TryGetOption(args, "--file", out var file) && File.Exists(file)))
        {
            Console.WriteLine("--file <path> is required.");
            break;
        }
        var bytes = await File.ReadAllBytesAsync(file);
        if (TryGetOption(args, "--lot", out var ls) && long.TryParse(ls, out var lotId))
        {
            var rec = sp.GetRequiredService<VinImageRecoveryService>();
            var res = await rec.ProcessLotImageAsync(lotId, bytes);
            Console.WriteLine($"Candidates={res.Candidates} recovered={(res.Recovered ? res.Vin : "(none)")} method={res.Method} conf={res.Confidence}");
        }
        else
        {
            var ext = sp.GetRequiredService<VinImageExtractor>();
            var pattern = TryGetOption(args, "--pattern", out var p) ? p : null;
            var cands = ext.ExtractCandidates(bytes);
            foreach (var c in cands)
                Console.WriteLine($"{c.Vin} method={c.Method} valid={c.CheckDigitValid} matches={VinValidator.MatchesMaskedPattern(c.Vin, pattern)} conf={c.Confidence}");
            Console.WriteLine($"best={ext.BestCandidate(cands, pattern)?.Vin ?? "(none)"}");
        }
        break;
    }

    default:
        Console.WriteLine(
            """
            CarPrices CLI
              db migrate                       Apply migrations + seed lookups
              import vincheck --sample         Import the bundled sample index (data/sample)
              import vincheck --file <path>    Import a local CSV/JSON index
              import vincheck [--url <url>]    Download + import the live index
              stats overview                   Print market overview
              makes                            List makes with observation counts
              targets sync                     Load collection targets from config into the DB
              targets list                     List collection targets
              targets add --make <m> [--model <m>] [--trim <t>] [--year-from <y>] [--year-to <y>] [--sources <a,b>]
              scrape copart                    Run Copart discovery for enabled targets (needs Playwright)
              scrape iaai                      Run IAAI discovery for enabled targets (needs Playwright)
              capture-close [iaai]             Re-check closed lots to record final prices
              enrich                           vPIC VIN decode + baseline feature extraction
              vin-from-image --file <path> [--lot <id>] [--pattern <11>]   Recover VIN from an image
            """);
        break;
}

static bool HasFlag(string[] args, string flag) =>
    args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

static bool TryGetOption(string[] args, string name, out string value)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            value = args[i + 1];
            return true;
        }
    }
    value = string.Empty;
    return false;
}
