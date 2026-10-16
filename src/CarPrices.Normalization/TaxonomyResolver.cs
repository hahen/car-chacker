using System.Globalization;
using System.Text;
using CarPrices.Domain.Entities;
using CarPrices.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarPrices.Normalization;

/// <summary>
/// Resolves raw make/model/trim strings (as spelled by a source) to canonical taxonomy ids,
/// using the alias tables. Unknowns are created on the fly when <c>autoCreate</c> is true.
/// </summary>
public class TaxonomyResolver(AppDbContext db)
{
    public record Resolution(long? MakerId, long? ModelId, long? TrimId);

    public async Task<Resolution> ResolveAsync(
        string? make, string? model, string? trim, bool autoCreate = true, CancellationToken ct = default)
    {
        var makerId = await ResolveMakerAsync(make, autoCreate, ct);
        long? modelId = null, trimId = null;
        if (makerId is not null && !string.IsNullOrWhiteSpace(model))
            modelId = await ResolveModelAsync(makerId.Value, model!, autoCreate, ct);
        if (modelId is not null && !string.IsNullOrWhiteSpace(trim))
            trimId = await ResolveTrimAsync(modelId.Value, trim!, autoCreate, ct);
        return new Resolution(makerId, modelId, trimId);
    }

    public async Task<long?> ResolveMakerAsync(string? make, bool autoCreate = true, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(make)) return null;
        var alias = make.Trim().ToUpperInvariant();
        var norm = Normalize(make);

        var byAlias = await db.MakerAliases.FirstOrDefaultAsync(a => a.Alias == alias, ct);
        if (byAlias is not null) return byAlias.MakerId;
        var byName = await db.Makers.FirstOrDefaultAsync(m => m.NormalizedName == norm, ct);
        if (byName is not null) return byName.Id;
        if (!autoCreate) return null;

        var maker = new Maker { Name = TitleCase(make), NormalizedName = norm };
        maker.Aliases.Add(new MakerAlias { Alias = alias, Source = "auto" });
        db.Makers.Add(maker);
        await db.SaveChangesAsync(ct);
        return maker.Id;
    }

    public async Task<long?> ResolveModelAsync(long makerId, string model, bool autoCreate = true, CancellationToken ct = default)
    {
        var alias = model.Trim().ToUpperInvariant();
        var norm = Normalize(model);

        var byAlias = await db.ModelAliases
            .FirstOrDefaultAsync(a => a.Alias == alias && a.Model!.MakerId == makerId, ct);
        if (byAlias is not null) return byAlias.ModelId;
        var byName = await db.Models.FirstOrDefaultAsync(m => m.MakerId == makerId && m.NormalizedName == norm, ct);
        if (byName is not null) return byName.Id;
        if (!autoCreate) return null;

        var m = new Model { MakerId = makerId, Name = TitleCase(model), NormalizedName = norm };
        m.Aliases.Add(new ModelAlias { Alias = alias, Source = "auto" });
        db.Models.Add(m);
        await db.SaveChangesAsync(ct);
        return m.Id;
    }

    public async Task<long?> ResolveTrimAsync(long modelId, string trim, bool autoCreate = true, CancellationToken ct = default)
    {
        var alias = trim.Trim().ToUpperInvariant();
        var norm = Normalize(trim);

        var byAlias = await db.TrimAliases
            .FirstOrDefaultAsync(a => a.Alias == alias && a.Trim!.ModelId == modelId, ct);
        if (byAlias is not null) return byAlias.TrimId;
        var byName = await db.Trims.FirstOrDefaultAsync(t => t.ModelId == modelId && t.NormalizedName == norm, ct);
        if (byName is not null) return byName.Id;
        if (!autoCreate) return null;

        var t = new Trim { ModelId = modelId, Name = trim.Trim(), NormalizedName = norm };
        db.Trims.Add(t);
        await db.SaveChangesAsync(ct);
        return t.Id;
    }

    /// <summary>Lowercase, single-spaced normalization used for canonical-name matching.</summary>
    public static string Normalize(string s)
    {
        var sb = new StringBuilder();
        var prevSpace = false;
        foreach (var c in s.Trim().ToLowerInvariant())
        {
            if (char.IsWhiteSpace(c))
            {
                if (!prevSpace) { sb.Append(' '); prevSpace = true; }
            }
            else { sb.Append(c); prevSpace = false; }
        }
        return sb.ToString().Trim();
    }

    private static string TitleCase(string s) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.Trim().ToLowerInvariant());
}
