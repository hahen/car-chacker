using System.Globalization;
using System.Text.Json;
using CarPrices.Domain.Entities;
using CarPrices.Domain.Enums;
using CarPrices.Infrastructure;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarPrices.Scraping.VinCheck;

/// <summary>
/// Imports the VinCheck aggregate auction price index (CSV or JSON) into the VinCheckIndex table.
/// Column/field names are matched flexibly so the importer tolerates small schema differences.
/// </summary>
public class VinCheckImporter(AppDbContext db, HttpClient http, ILogger<VinCheckImporter> logger)
{
    public record ImportResult(int Rows, int Inserted, int Updated, int Skipped);

    public async Task<ImportResult> ImportFromUrlAsync(string url, CancellationToken ct = default)
    {
        logger.LogInformation("Downloading VinCheck index from {Url}", url);
        var content = await http.GetStringAsync(url, ct);
        return await ImportFromTextAsync(content, GuessIsJson(url, content), ct);
    }

    public async Task<ImportResult> ImportFromFileAsync(string path, CancellationToken ct = default)
    {
        logger.LogInformation("Reading VinCheck index from file {Path}", path);
        var content = await File.ReadAllTextAsync(path, ct);
        return await ImportFromTextAsync(content, GuessIsJson(path, content), ct);
    }

    public async Task<ImportResult> ImportFromTextAsync(string content, bool isJson, CancellationToken ct = default)
    {
        var rows = isJson ? ParseJson(content) : ParseCsv(content);
        return await UpsertAsync(rows, ct);
    }

    private static bool GuessIsJson(string nameOrUrl, string content)
    {
        if (nameOrUrl.Contains(".json", StringComparison.OrdinalIgnoreCase)) return true;
        if (nameOrUrl.Contains(".csv", StringComparison.OrdinalIgnoreCase)) return false;
        var trimmed = content.TrimStart();
        return trimmed.StartsWith('[') || trimmed.StartsWith('{');
    }

    private List<VinCheckIndex> ParseCsv(string content)
    {
        var result = new List<VinCheckIndex>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = a => a.Header.ToLowerInvariant().Trim().Replace("_", "").Replace(" ", ""),
        };
        using var reader = new StringReader(content);
        using var csv = new CsvReader(reader, config);
        csv.Read();
        csv.ReadHeader();
        while (csv.Read())
        {
            var year = GetInt(csv, "year", "modelyear");
            var make = GetStr(csv, "make", "maker", "brand");
            var model = GetStr(csv, "model");
            if (year is null || string.IsNullOrWhiteSpace(make) || string.IsNullOrWhiteSpace(model)) continue;
            result.Add(new VinCheckIndex
            {
                ModelYear = year.Value,
                Maker = make!.Trim(),
                Model = model!.Trim(),
                SampleSize = GetInt(csv, "samplesize", "n", "count", "sales", "observations") ?? 0,
                P25 = GetDec(csv, "p25", "pct25", "percentile25", "price25"),
                P50 = GetDec(csv, "p50", "median", "pct50", "percentile50", "price50"),
                P75 = GetDec(csv, "p75", "pct75", "percentile75", "price75"),
                GeneratedAtUtc = GetDate(csv, "generatedat", "generated", "asof"),
            });
        }
        return result;
    }

    private List<VinCheckIndex> ParseJson(string content)
    {
        var result = new List<VinCheckIndex>();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        JsonElement array;
        if (root.ValueKind == JsonValueKind.Array)
        {
            array = root;
        }
        else
        {
            // find the first array property (data/rows/index/...)
            array = default;
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array) { array = prop.Value; break; }
            }
            if (array.ValueKind != JsonValueKind.Array) return result;
        }

        foreach (var el in array.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.Object) continue;
            var map = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in el.EnumerateObject())
                map[p.Name.ToLowerInvariant().Replace("_", "").Replace(" ", "")] = p.Value;

            var year = JInt(map, "year", "modelyear");
            var make = JStr(map, "make", "maker", "brand");
            var model = JStr(map, "model");
            if (year is null || string.IsNullOrWhiteSpace(make) || string.IsNullOrWhiteSpace(model)) continue;
            result.Add(new VinCheckIndex
            {
                ModelYear = year.Value,
                Maker = make!.Trim(),
                Model = model!.Trim(),
                SampleSize = JInt(map, "samplesize", "n", "count", "sales", "observations") ?? 0,
                P25 = JDec(map, "p25", "pct25", "percentile25", "price25"),
                P50 = JDec(map, "p50", "median", "pct50", "percentile50", "price50"),
                P75 = JDec(map, "p75", "pct75", "percentile75", "price75"),
                GeneratedAtUtc = JDate(map, "generatedat", "generated", "asof"),
            });
        }
        return result;
    }

    private async Task<ImportResult> UpsertAsync(List<VinCheckIndex> rows, CancellationToken ct)
    {
        int inserted = 0, updated = 0, skipped = 0;
        var existing = await db.VinCheckIndex.ToListAsync(ct);
        var byKey = existing.ToDictionary(
            x => (x.ModelYear, x.Maker.ToLowerInvariant(), x.Model.ToLowerInvariant()));

        foreach (var r in rows)
        {
            var key = (r.ModelYear, r.Maker.ToLowerInvariant(), r.Model.ToLowerInvariant());
            if (byKey.TryGetValue(key, out var e))
            {
                e.SampleSize = r.SampleSize;
                e.P25 = r.P25; e.P50 = r.P50; e.P75 = r.P75;
                e.GeneratedAtUtc = r.GeneratedAtUtc;
                e.ImportedAtUtc = DateTime.UtcNow;
                updated++;
            }
            else
            {
                db.VinCheckIndex.Add(r);
                byKey[key] = r;
                inserted++;
            }
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("VinCheck import: {Rows} rows ({Inserted} new, {Updated} updated)", rows.Count, inserted, updated);
        return new ImportResult(rows.Count, inserted, updated, skipped);
    }

    // --- CSV field helpers ---
    private static string? GetStr(CsvReader csv, params string[] names)
    {
        foreach (var n in names)
            if (csv.TryGetField<string>(n, out var v) && !string.IsNullOrWhiteSpace(v)) return v;
        return null;
    }

    private static int? GetInt(CsvReader csv, params string[] names)
    {
        var s = GetStr(csv, names);
        return int.TryParse(CleanNumber(s), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static decimal? GetDec(CsvReader csv, params string[] names)
    {
        var s = GetStr(csv, names);
        return decimal.TryParse(CleanNumber(s), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static DateTime? GetDate(CsvReader csv, params string[] names)
    {
        var s = GetStr(csv, names);
        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var v) ? v : null;
    }

    // --- JSON field helpers ---
    private static string? JStr(Dictionary<string, JsonElement> m, params string[] names)
    {
        foreach (var n in names)
            if (m.TryGetValue(n, out var e))
                return e.ValueKind == JsonValueKind.String ? e.GetString() : e.ToString();
        return null;
    }

    private static int? JInt(Dictionary<string, JsonElement> m, params string[] names)
    {
        foreach (var n in names)
            if (m.TryGetValue(n, out var e))
            {
                if (e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var i)) return i;
                if (int.TryParse(CleanNumber(e.ToString()), NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
            }
        return null;
    }

    private static decimal? JDec(Dictionary<string, JsonElement> m, params string[] names)
    {
        foreach (var n in names)
            if (m.TryGetValue(n, out var e))
            {
                if (e.ValueKind == JsonValueKind.Number && e.TryGetDecimal(out var d)) return d;
                if (decimal.TryParse(CleanNumber(e.ToString()), NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
            }
        return null;
    }

    private static DateTime? JDate(Dictionary<string, JsonElement> m, params string[] names)
    {
        var s = JStr(m, names);
        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var v) ? v : null;
    }

    private static string? CleanNumber(string? s) =>
        string.IsNullOrWhiteSpace(s) ? s : s.Replace("$", "").Replace(",", "").Trim();
}
