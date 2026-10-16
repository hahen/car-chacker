using CarPrices.Domain.Entities;
using CarPrices.Domain.Enums;
using CarPrices.Infrastructure;
using CarPrices.Scraping.Collection;
using CarPrices.Scraping.Ingestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarPrices.Scraping.Copart;

public record ScrapeRunSummary(long RunId, int LotsSeen, int New, int Updated, int PricesCaptured, int Errors);

/// <summary>
/// Orchestrates Copart collection: discovery (search each enabled target, upsert lots) and
/// close-capture (re-check lots whose auction has passed to record the final price). Each run is
/// logged as a ScrapeRun. The browser session is created per run and disposed after.
/// </summary>
public class CopartCollector(
    AppDbContext db,
    LotIngestionService ingest,
    CollectionTargetService targets,
    IOptions<CopartOptions> copartOptions,
    IOptions<CollectionOptions> collectionOptions,
    ILogger<CopartCollector> logger)
{
    public async Task<ScrapeRunSummary> RunDiscoveryAsync(CancellationToken ct = default)
    {
        if (!await db.CollectionTargets.AnyAsync(ct))
            await targets.SyncFromConfigAsync(ct);

        var run = new ScrapeRun { Source = Source.Copart, JobType = "discovery" };
        db.ScrapeRuns.Add(run);
        await db.SaveChangesAsync(ct);

        int seen = 0, added = 0, updated = 0, errors = 0;
        try
        {
            var enabled = await targets.GetEnabledAsync(Source.Copart, ct);
            if (enabled.Count == 0)
                logger.LogWarning("No enabled Copart collection targets — nothing to collect.");

            await using var client = new CopartClient(copartOptions.Value, logger);
            await client.StartAsync(ct);

            foreach (var t in enabled)
            {
                var keyword = CopartSearchPayloadBuilder.BuildKeyword(t.MakeText, t.ModelText, t.TrimText);
                logger.LogInformation("Target: '{Keyword}' years {From}-{To}", keyword, t.YearFrom, t.YearTo);
                var lots = await client.SearchAsync(keyword, ct, collectionOptions.Value.MaxPagesPerSearch);
                foreach (var lot in lots)
                {
                    if (!MatchesTarget(lot, t)) continue;
                    seen++;
                    try
                    {
                        var outcome = await ingest.IngestAsync(lot, Source.Copart, run.Id, ct);
                        if (outcome.IsNew) added++; else updated++;
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        await LogErrorAsync(run.Id, "ingest", lot.LotNumber, ex, ct);
                    }
                }
            }
            run.Status = "Completed";
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.Notes = ex.Message;
            errors++;
            logger.LogError(ex, "Copart discovery run failed");
        }
        finally
        {
            run.LotsSeen = seen;
            run.LotsNew = added;
            run.LotsUpdated = updated;
            run.ErrorCount = errors;
            run.FinishedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return new ScrapeRunSummary(run.Id, seen, added, updated, 0, errors);
    }

    public async Task<ScrapeRunSummary> RunCloseCaptureAsync(int maxLots = 200, CancellationToken ct = default)
    {
        var run = new ScrapeRun { Source = Source.Copart, JobType = "close-capture" };
        db.ScrapeRuns.Add(run);
        await db.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;
        int seen = 0, captured = 0, updated = 0, errors = 0;

        var due = await db.Lots
            .Where(l => l.Source == Source.Copart && l.AuctionDateUtc != null && l.AuctionDateUtc < now
                        && !l.IsSold && l.SaleStatus != SaleStatus.Sold && l.SaleStatus != SaleStatus.Cancelled)
            .OrderBy(l => l.AuctionDateUtc)
            .Take(maxLots)
            .ToListAsync(ct);

        try
        {
            if (due.Count > 0)
            {
                await using var client = new CopartClient(copartOptions.Value, logger);
                await client.StartAsync(ct);

                foreach (var lot in due)
                {
                    seen++;
                    try
                    {
                        var fresh = await client.GetLotByNumberAsync(lot.SourceLotNumber, ct);
                        if (fresh is null)
                        {
                            // Delisted after its auction → most likely sold; record last known bid as final.
                            if (lot.CurrentBid is > 0)
                            {
                                lot.SaleStatus = SaleStatus.Sold;
                                lot.IsSold = true;
                                lot.FinalBid = lot.CurrentBid;
                                lot.SalePrice = lot.CurrentBid;
                                captured++;
                            }
                            else { lot.SaleStatus = SaleStatus.NotSold; updated++; }
                            lot.SaleCapturedAtUtc = now;
                        }
                        else
                        {
                            lot.CurrentBid = fresh.CurrentBid ?? lot.CurrentBid;
                            var final = ClassifyFinal(fresh, now);
                            if (final == SaleStatus.Sold)
                            {
                                lot.SaleStatus = SaleStatus.Sold;
                                lot.IsSold = true;
                                lot.FinalBid = fresh.CurrentBid ?? lot.CurrentBid;
                                lot.SalePrice = lot.FinalBid;
                                lot.SaleCapturedAtUtc = now;
                                captured++;
                            }
                            else if (final == SaleStatus.NotSold)
                            {
                                lot.SaleStatus = SaleStatus.NotSold;
                                lot.SaleCapturedAtUtc = now;
                                updated++;
                            }
                            else { updated++; }
                        }
                        lot.LastSeenUtc = now;
                        await db.SaveChangesAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        await LogErrorAsync(run.Id, "close-capture", lot.SourceLotNumber, ex, ct);
                    }
                }
            }
            run.Status = "Completed";
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.Notes = ex.Message;
            logger.LogError(ex, "Copart close-capture run failed");
        }
        finally
        {
            run.LotsSeen = seen;
            run.PricesCaptured = captured;
            run.LotsUpdated = updated;
            run.ErrorCount = errors;
            run.FinishedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return new ScrapeRunSummary(run.Id, seen, 0, updated, captured, errors);
    }

    private static bool MatchesTarget(ScrapedLot lot, CollectionTarget t)
    {
        if (t.YearFrom is int yf && (lot.Year is null || lot.Year < yf)) return false;
        if (t.YearTo is int yt && (lot.Year is null || lot.Year > yt)) return false;
        if (!string.IsNullOrWhiteSpace(t.Regions))
        {
            var regions = t.Regions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(r => r.ToUpperInvariant());
            if (lot.LocationState is null || !regions.Contains(lot.LocationState.ToUpperInvariant())) return false;
        }
        return true;
    }

    /// <summary>Heuristic end-of-sale classification — tune against live Copart status codes.</summary>
    private static SaleStatus? ClassifyFinal(ScrapedLot s, DateTime now)
    {
        if (string.Equals(s.LotStatus, "S", StringComparison.OrdinalIgnoreCase)) return SaleStatus.Sold;
        if (s.AuctionDateUtc is DateTime ad && ad < now)
            return s.CurrentBid is > 0 ? SaleStatus.Sold : SaleStatus.NotSold;
        return null;
    }

    private async Task LogErrorAsync(long runId, string stage, string? lotNumber, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "{Stage} failed for lot {Lot}", stage, lotNumber);
        db.IngestErrors.Add(new IngestError
        {
            ScrapeRunId = runId,
            Stage = stage,
            SourceLotNumber = lotNumber,
            Message = ex.Message,
        });
        await db.SaveChangesAsync(ct);
    }
}
