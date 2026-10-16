using CarPrices.Domain.Entities;
using CarPrices.Domain.Enums;
using CarPrices.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarPrices.Scraping.Collection;

/// <summary>Seeds collection targets from config into the DB and reads the enabled set per source.</summary>
public class CollectionTargetService(AppDbContext db, IOptions<CollectionOptions> options)
{
    /// <summary>Upserts the config targets into the CollectionTargets table. Returns rows added.</summary>
    public async Task<int> SyncFromConfigAsync(CancellationToken ct = default)
    {
        var added = 0;
        foreach (var t in options.Value.Targets)
        {
            var make = t.Make?.Trim();
            if (string.IsNullOrWhiteSpace(make)) continue;
            var model = string.IsNullOrWhiteSpace(t.Model) ? null : t.Model!.Trim();

            var existing = await db.CollectionTargets
                .FirstOrDefaultAsync(x => x.MakeText == make && x.ModelText == model && x.YearFrom == t.YearFrom, ct);

            if (existing is null)
            {
                db.CollectionTargets.Add(new CollectionTarget
                {
                    MakeText = make,
                    ModelText = model,
                    TrimText = t.Trim,
                    YearFrom = t.YearFrom,
                    YearTo = t.YearTo,
                    Sources = Join(t.Sources),
                    TitleGroups = Join(t.TitleGroups),
                    Regions = Join(t.Regions),
                    Enabled = t.Enabled,
                });
                added++;
            }
            else
            {
                existing.TrimText = t.Trim;
                existing.YearTo = t.YearTo;
                existing.Sources = Join(t.Sources);
                existing.TitleGroups = Join(t.TitleGroups);
                existing.Regions = Join(t.Regions);
                existing.Enabled = t.Enabled;
            }
        }
        await db.SaveChangesAsync(ct);
        return added;
    }

    public async Task<List<CollectionTarget>> GetEnabledAsync(Source source, CancellationToken ct = default)
    {
        var all = await db.CollectionTargets.Where(t => t.Enabled).ToListAsync(ct);
        return all
            .Where(t => string.IsNullOrWhiteSpace(t.Sources) ||
                        t.Sources.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                 .Any(x => string.Equals(x, source.ToString(), StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public Task<List<CollectionTarget>> GetAllAsync(CancellationToken ct = default) =>
        db.CollectionTargets.OrderBy(t => t.MakeText).ThenBy(t => t.ModelText).ToListAsync(ct);

    private static string? Join(string[]? values) =>
        values is { Length: > 0 } ? string.Join(',', values) : null;
}
