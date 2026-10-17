using CarPrices.Domain.Entities;
using CarPrices.Domain.Enums;
using CarPrices.Infrastructure;
using CarPrices.Scraping.Ingestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarPrices.Scraping.History;

/// <summary>
/// Scrapes historical sold prices from carsbidshistory.com for a make+model: resolves the make id,
/// finds matching models (trims), paginates each model's listing, fetches car detail pages, and
/// ingests each sold bid-history row as a sold Lot (which then feeds PriceStats / the estimator).
/// </summary>
public class CarsBidsHistoryCollector(
    AppDbContext db,
    LotIngestionService ingest,
    CarsBidsHistoryClient client,
    ILogger<CarsBidsHistoryCollector> logger)
{
    public async Task<ScrapeRunSummary> RunAsync(string make, string model, int? maxCars = null,
        int? yearFrom = null, int? yearTo = null, CancellationToken ct = default)
    {
        var run = new ScrapeRun { Source = Source.CarsBidsHistory, JobType = "history" };
        db.ScrapeRuns.Add(run);
        await db.SaveChangesAsync(ct);

        int seen = 0, added = 0, updated = 0, errors = 0;
        var opts = client.Options;
        try
        {
            var makeUrl = ToUrlName(make);

            // 1) resolve make id by scanning the makes list
            string? makeId = null;
            for (var p = 1; p <= opts.MaxMakePages && makeId is null; p++)
            {
                var html = await client.GetHtmlAsync($"/make/page-{p}", ct);
                if (html is not null) makeId = CarsBidsHistoryParser.FindMakeId(html, makeUrl);
                await client.JitterAsync(ct);
            }
            if (makeId is null)
            {
                run.Notes = $"make '{make}' not found";
                logger.LogWarning("carsbidshistory: make '{Make}' not found", make);
            }
            else
            {
                // 2) find matching models (trims)
                var makeHtml = await client.GetHtmlAsync($"/make/{makeId}-{makeUrl}", ct) ?? string.Empty;
                var tokens = model.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                var models = CarsBidsHistoryParser.FindModels(makeHtml, makeId, tokens).Take(opts.MaxModelsPerTarget).ToList();
                logger.LogInformation("carsbidshistory {Make} (id {Id}): {N} model(s) matching '{Model}'", make, makeId, models.Count, model);

                var cap = maxCars ?? opts.MaxCarsPerModel;
                foreach (var (modelId, modelName) in models)
                {
                    var trim = modelName.Replace('_', ' ').Trim();

                    // 3) collect detail URLs across listing pages, filter by model year
                    var detailUrls = new List<string>();
                    for (var page = 1; page <= opts.MaxListingPages; page++)
                    {
                        var listHtml = await client.GetHtmlAsync($"/make/{makeId}-{makeUrl}/{modelId}-{modelName}/page-{page}", ct);
                        if (listHtml is null) break;
                        var fresh = CarsBidsHistoryParser.ParseListingDetailUrls(listHtml).Where(u => !detailUrls.Contains(u)).ToList();
                        if (fresh.Count == 0) break;
                        detailUrls.AddRange(fresh);
                        await client.JitterAsync(ct);
                    }

                    var inRange = detailUrls
                        .Where(u => InYear(CarsBidsHistoryParser.ParseIdentity(u).Year, yearFrom, yearTo))
                        .Take(cap).ToList();

                    // 4) fetch each car detail page and ingest its sold rows
                    foreach (var detailUrl in inRange)
                    {
                        try
                        {
                            var detailHtml = await client.GetHtmlAsync(detailUrl, ct);
                            if (detailHtml is null) continue;
                            var identity = CarsBidsHistoryParser.ParseIdentity(detailUrl) with { Make = make, Model = model, Trim = trim };
                            foreach (var lot in CarsBidsHistoryParser.ParseDetail(detailHtml, identity))
                            {
                                seen++;
                                try
                                {
                                    var outcome = await ingest.IngestAsync(lot, Source.CarsBidsHistory, run.Id, ct);
                                    if (outcome.IsNew) added++; else updated++;
                                }
                                catch (Exception ex) { errors++; await LogErrorAsync(run.Id, "ingest", lot.LotNumber, ex, ct); }
                            }
                            await client.JitterAsync(ct);
                        }
                        catch (Exception ex) { errors++; await LogErrorAsync(run.Id, "detail", detailUrl, ex, ct); }
                    }
                }
            }
            run.Status = "Completed";
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.Notes = ex.Message;
            logger.LogError(ex, "carsbidshistory history run failed");
        }
        finally
        {
            run.LotsSeen = seen;
            run.LotsNew = added;
            run.LotsUpdated = updated;
            run.PricesCaptured = added;
            run.ErrorCount = errors;
            run.FinishedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return new ScrapeRunSummary(run.Id, seen, added, updated, added, errors);
    }

    private static string ToUrlName(string s) => s.Trim().ToUpperInvariant().Replace(' ', '_').Replace('-', '_');

    private static bool InYear(int? year, int? from, int? to) =>
        (from is null || (year is int y1 && y1 >= from)) && (to is null || (year is int y2 && y2 <= to));

    private async Task LogErrorAsync(long runId, string stage, string? key, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "{Stage} failed: {Key}", stage, key);
        db.IngestErrors.Add(new IngestError { ScrapeRunId = runId, Stage = stage, SourceLotNumber = key, Message = ex.Message });
        await db.SaveChangesAsync(ct);
    }
}
