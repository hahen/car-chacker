using System.Text.RegularExpressions;
using CarPrices.Domain.Entities;
using CarPrices.Domain.Enums;
using CarPrices.Enrichment.Vpic;
using CarPrices.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarPrices.Enrichment.Features;

/// <summary>
/// Baseline feature extraction: maps vPIC structured safety/drivetrain fields and free-text options
/// (via the FeatureMappings table) to normalized CarFeatures. Deduped by (CarId, FeatureId, Source).
/// </summary>
public class FeatureExtractor(AppDbContext db)
{
    private Dictionary<string, long>? _featureIdByCode;
    private List<FeatureMapping>? _mappings;

    public async Task<int> ExtractFromVpicAsync(Car car, VpicResult r, CancellationToken ct = default)
    {
        await EnsureAsync(ct);
        var have = await LoadHaveAsync(car.Id, ct);
        var added = 0;

        void Add(string code, double conf)
        {
            if (!_featureIdByCode!.TryGetValue(code, out var fid)) return;
            if (!have.Add((fid, FeatureSource.VinDecode))) return;
            db.CarFeatures.Add(new CarFeature { CarId = car.Id, FeatureId = fid, Source = FeatureSource.VinDecode, Confidence = conf });
            added++;
        }

        double Conf(string field)
        {
            var v = r.Get(field)?.ToUpperInvariant() ?? "";
            if (v.Contains("STANDARD")) return 1.0;
            if (v.Contains("OPTION")) return 0.6;
            return 0.9;
        }

        if (r.IsPresent("AdaptiveCruiseControl")) Add("ADAPTIVE_CRUISE", Conf("AdaptiveCruiseControl"));
        if (r.IsPresent("BlindSpotMon") || r.IsPresent("BlindSpotIntervention")) Add("BLIND_SPOT", 0.9);
        if (r.IsPresent("LaneKeepSystem") || r.IsPresent("LaneDepartureWarning")) Add("LANE_KEEP", 0.9);
        if (r.IsPresent("RearVisibilitySystem")) Add("BACKUP_CAMERA", Conf("RearVisibilitySystem"));
        if (r.IsPresent("ParkAssist")) Add("PARKING_SENSORS", 0.8);
        if (r.IsPresent("ForwardCollisionWarning") || r.IsPresent("CIB") || r.IsPresent("DynamicBrakeSupport")) Add("AEB", 0.9);
        if (r.IsPresent("KeylessIgnition")) { Add("KEYLESS_ENTRY", 0.9); Add("PUSH_BUTTON_START", 0.8); }

        var dt = r.DriveType?.ToUpperInvariant();
        if (dt is not null)
        {
            if (dt.Contains("ALL") || dt.Contains("AWD")) Add("AWD", 1.0);
            else if (dt.Contains("4WD") || dt.Contains("4X4") || dt.Contains("FOUR")) Add("FOUR_WD", 1.0);
            else if (dt.Contains("FRONT") || dt == "FWD") Add("FWD", 1.0);
            else if (dt.Contains("REAR") || dt == "RWD") Add("RWD", 1.0);
        }

        await db.SaveChangesAsync(ct);
        return added;
    }

    /// <summary>Maps free text (listing description / option list) to features via FeatureMappings.</summary>
    public async Task<int> ExtractFromTextAsync(Car car, string? text, FeatureSource source, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        await EnsureAsync(ct);
        var have = await LoadHaveAsync(car.Id, ct);
        var lower = text.ToLowerInvariant();
        var added = 0;

        foreach (var m in _mappings!.Where(x => x.MakerId is null || x.MakerId == car.MakerId).OrderByDescending(x => x.Priority))
        {
            var pattern = m.Pattern.ToLowerInvariant();
            var match = m.MatchType switch
            {
                FeatureMatchType.Exact => lower == pattern,
                FeatureMatchType.Contains => lower.Contains(pattern),
                FeatureMatchType.Regex => Regex.IsMatch(lower, m.Pattern, RegexOptions.IgnoreCase),
                _ => false,
            };
            if (match && have.Add((m.FeatureId, source)))
            {
                db.CarFeatures.Add(new CarFeature { CarId = car.Id, FeatureId = m.FeatureId, Source = source, Confidence = 0.7 });
                added++;
            }
        }

        await db.SaveChangesAsync(ct);
        return added;
    }

    private async Task<HashSet<(long, FeatureSource)>> LoadHaveAsync(long carId, CancellationToken ct)
    {
        var existing = await db.CarFeatures.Where(f => f.CarId == carId)
            .Select(f => new { f.FeatureId, f.Source }).ToListAsync(ct);
        return existing.Select(e => (e.FeatureId, e.Source)).ToHashSet();
    }

    private async Task EnsureAsync(CancellationToken ct)
    {
        _featureIdByCode ??= await db.Features.ToDictionaryAsync(f => f.Code, f => f.Id, ct);
        _mappings ??= await db.FeatureMappings.ToListAsync(ct);
    }
}
