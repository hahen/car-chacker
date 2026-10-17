using System.Text.RegularExpressions;
using CarPrices.Domain.Entities;
using CarPrices.Domain.Enums;
using CarPrices.Infrastructure;
using CarPrices.Scraping.Collection;
using CarPrices.Scraping.Ingestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarPrices.Scraping.Iaai;

/// <summary>
/// Orchestrates IAAI collection (discovery + close-capture), scoped by collection targets, mirroring
/// the Copart collector. Close-capture is best-effort (detail-page text heuristic) and needs tuning.
/// </summary>
public class IaaiCollector(
    AppDbContext db,
    LotIngestionService ingest,
    CollectionTargetService targets,
    IOptions<IaaiOptions> iaaiOptions,
    ILogger<IaaiCollector> logger)
{
    public async Task<ScrapeRunSummary> RunDiscoveryAsync(CancellationToken ct = default)
    {
        if (!await db.CollectionTargets.AnyAsync(ct))
            await targets.SyncFromConfigAsync(ct);

        var run = new ScrapeRun { Source = Source.Iaai, JobType = "discovery" };
        db.ScrapeRuns.Add(run);
        await db.SaveChangesAsync(ct);

        int seen = 0, added = 0, updated = 0, errors = 0;
        try
        {
            var enabled = await targets.GetEnabledAsync(Source.Iaai, ct);
            if (enabled.Count == 0)
                logger.LogWarning("No enabled IAAI collection targets — nothing to collect.");

            await using var client = new IaaiClient(iaaiOptions.Value, logger);
            await client.StartAsync(ct);

            foreach (var t in enabled)
            {
                var keyword = BuildKeyword(t);
                logger.LogInformation("IAAI target: '{Keyword}' years {From}-{To}", keyword, t.YearFrom, t.YearTo);
                var lots = await client.SearchAsync(keyword, ct);
                foreach (var lot in lots)
                {
                    if (!MatchesTarget(lot, t)) continue;
                    seen++;
                    try
                    {
                        var outcome = await ingest.IngestAsync(lot, Source.Iaai, run.Id, ct);
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
            logger.LogError(ex, "IAAI discovery run failed");
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

    public async Task<ScrapeRunSummary> RunCloseCaptureAsync(int maxLots = 100, CancellationToken ct = default)
    {
        var run = new ScrapeRun { Source = Source.Iaai, JobType = "close-capture" };
        db.ScrapeRuns.Add(run);
        await db.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;
        int seen = 0, captured = 0, updated = 0, errors = 0;

        var due = await db.Lots
            .Where(l => l.Source == Source.Iaai && l.AuctionDateUtc != null && l.AuctionDateUtc < now
                        && !l.IsSold && l.SaleStatus != SaleStatus.Sold && l.SaleStatus != SaleStatus.Cancelled
                        && l.LotUrl != null)
            .OrderBy(l => l.AuctionDateUtc)
            .Take(maxLots)
            .ToListAsync(ct);

        try
        {
            if (due.Count > 0)
            {
                await using var client = new IaaiClient(iaaiOptions.Value, logger);
                await client.StartAsync(ct);

                foreach (var lot in due)
                {
                    seen++;
                    try
                    {
                        var text = await client.GetDetailTextAsync(lot.LotUrl!, ct);
                        var (sold, price) = DetectSold(text);
                        if (sold)
                        {
                            lot.SaleStatus = SaleStatus.Sold;
                            lot.IsSold = true;
                            lot.FinalBid = price ?? lot.CurrentBid;
                            lot.SalePrice = lot.FinalBid;
                            captured++;
                        }
                        else { updated++; }
                        lot.SaleCapturedAtUtc = now;
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
            logger.LogError(ex, "IAAI close-capture run failed");
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

    /// <summary>Heuristic sold detection from a detail page's text — tune against live IAAI pages.</summary>
    public static (bool Sold, decimal? Price) DetectSold(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return (false, null);
        var sold = text.Contains("Winning Bid", StringComparison.OrdinalIgnoreCase)
                   || text.Contains("Sale Price", StringComparison.OrdinalIgnoreCase)
                   || Regex.IsMatch(text, @"\bSold\b", RegexOptions.IgnoreCase);
        decimal? price = null;
        var m = Regex.Match(text, @"(?:Winning Bid|Sale Price|Sold(?:\s*Price)?)[^$]{0,40}\$\s*([\d,]+)", RegexOptions.IgnoreCase);
        if (m.Success && decimal.TryParse(m.Groups[1].Value.Replace(",", ""), out var d)) price = d;
        return (sold, price);
    }

    private static string BuildKeyword(CollectionTarget t) =>
        string.Join(' ', new[] { t.MakeText, t.ModelText, t.TrimText }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

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

    private async Task LogErrorAsync(long runId, string stage, string? lotNumber, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "{Stage} failed for lot {Lot}", stage, lotNumber);
        db.IngestErrors.Add(new IngestError { ScrapeRunId = runId, Stage = stage, SourceLotNumber = lotNumber, Message = ex.Message });
        await db.SaveChangesAsync(ct);
    }
}
