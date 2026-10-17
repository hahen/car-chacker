using CarPrices.Enrichment;
using CarPrices.Infrastructure;
using CarPrices.Scraping;
using CarPrices.Scraping.Collection;
using CarPrices.Scraping.Copart;
using CarPrices.Scraping.Iaai;
using CarPrices.Scraping.VinCheck;
using CarPrices.Statistics;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddStatistics();
builder.Services.AddScraping(builder.Configuration);
builder.Services.AddEnrichment(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddPolicy("spa", p => p
    .WithOrigins("http://localhost:5173", "http://localhost:4173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// Ensure the database is migrated + seeded on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(db);
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors("spa");
app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

api.MapGet("/stats/overview", async (StatisticsService stats, CancellationToken ct) =>
    Results.Ok(await stats.GetMarketOverviewAsync(ct)));

api.MapGet("/stats/makes-summary", async (StatisticsService stats, CancellationToken ct) =>
    Results.Ok(await stats.GetMakeSummariesAsync(ct)));

api.MapGet("/makes", async (StatisticsService stats, CancellationToken ct) =>
    Results.Ok(await stats.GetMakesAsync(ct)));

api.MapGet("/makes/{make}/models", async (string make, StatisticsService stats, CancellationToken ct) =>
    Results.Ok(await stats.GetModelsAsync(make, ct)));

api.MapGet("/stats/series", async (string make, string model, StatisticsService stats, CancellationToken ct) =>
    Results.Ok(await stats.GetModelSeriesAsync(make, model, ct)));

api.MapPost("/import/vincheck/sample", async (VinCheckImporter importer, CancellationToken ct) =>
{
    var path = Path.Combine(CarDbPaths.DataDirectory(), "sample", "vincheck-sample.csv");
    if (!File.Exists(path))
        return Results.NotFound(new { error = "sample file not found", path });
    var result = await importer.ImportFromFileAsync(path, ct);
    return Results.Ok(result);
});

api.MapPost("/import/vincheck", async (string url, VinCheckImporter importer, CancellationToken ct) =>
    Results.Ok(await importer.ImportFromUrlAsync(url, ct)));

// Collection targets + Copart scraping (scraping is long-running — prefer the CLI/Worker for bulk runs).
api.MapGet("/targets", async (CollectionTargetService svc, CancellationToken ct) =>
    Results.Ok(await svc.GetAllAsync(ct)));

api.MapPost("/targets/sync", async (CollectionTargetService svc, CancellationToken ct) =>
    Results.Ok(new { added = await svc.SyncFromConfigAsync(ct) }));

api.MapPost("/scrape/copart/discovery", async (CopartCollector collector, CancellationToken ct) =>
    Results.Ok(await collector.RunDiscoveryAsync(ct)));

api.MapPost("/scrape/copart/close-capture", async (CopartCollector collector, CancellationToken ct) =>
    Results.Ok(await collector.RunCloseCaptureAsync(200, ct)));

api.MapPost("/scrape/iaai/discovery", async (IaaiCollector collector, CancellationToken ct) =>
    Results.Ok(await collector.RunDiscoveryAsync(ct)));

api.MapPost("/scrape/iaai/close-capture", async (IaaiCollector collector, CancellationToken ct) =>
    Results.Ok(await collector.RunCloseCaptureAsync(100, ct)));

api.MapPost("/enrich", async (EnrichmentService svc, CancellationToken ct) =>
    Results.Ok(await svc.RunAsync(200, ct)));

// SPA fallback (serves wwwroot/index.html when a React build is present).
app.MapFallbackToFile("index.html");

app.Run();
