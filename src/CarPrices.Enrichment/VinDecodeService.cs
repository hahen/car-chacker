using System.Text.Json;
using CarPrices.Domain.Entities;
using CarPrices.Enrichment.Vpic;
using CarPrices.Infrastructure;
using CarPrices.Normalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarPrices.Enrichment;

/// <summary>Decodes a VIN/pattern via vPIC (cached in VinDecodes) and applies specs to a Car.</summary>
public class VinDecodeService(
    AppDbContext db,
    VpicClient vpic,
    TaxonomyResolver taxonomy,
    ILogger<VinDecodeService> logger)
{
    public async Task<VpicResult?> GetOrDecodeAsync(string vinOrPattern, int? year, CancellationToken ct = default)
    {
        var cached = await db.VinDecodes.FirstOrDefaultAsync(d => d.VinOrPattern == vinOrPattern, ct);
        if (cached?.RawJson is not null)
        {
            var hit = VpicResponseParser.Parse(cached.RawJson).FirstOrDefault();
            if (hit is not null) return hit;
        }

        var res = await vpic.DecodeAsync(vinOrPattern, year, ct);
        if (res is null) return null;

        var entity = cached ?? new VinDecode { VinOrPattern = vinOrPattern };
        entity.Source = "vPIC";
        entity.DecodedAtUtc = DateTime.UtcNow;
        entity.RawJson = JsonSerializer.Serialize(new { Results = new[] { res.Fields } });
        entity.MakerName = res.Make;
        entity.ModelName = res.Model;
        entity.TrimName = res.Trim;
        entity.ModelYear = res.ModelYear;
        entity.BodyClass = res.BodyClass;
        entity.DriveType = res.DriveType;
        entity.FuelType = res.FuelType;
        entity.EngineModel = res.EngineModel;
        entity.PlantCountry = res.PlantCountry;
        if (cached is null) db.VinDecodes.Add(entity);
        await db.SaveChangesAsync(ct);
        return res;
    }

    public async Task ApplyToCarAsync(Car car, VpicResult r, CancellationToken ct = default)
    {
        car.BodyStyle ??= r.BodyClass;
        car.DriveType ??= r.DriveType;
        car.FuelType ??= r.FuelType;
        car.EngineDescription ??= r.EngineModel;
        car.EngineCylinders ??= r.EngineCylinders;
        car.EngineDisplacement ??= r.DisplacementL;
        car.ModelYear ??= r.ModelYear;

        if (car.MakerId is null && r.Make is not null)
        {
            var res = await taxonomy.ResolveAsync(r.Make, r.Model, r.Trim, autoCreate: true, ct);
            car.MakerId ??= res.MakerId;
            car.ModelId ??= res.ModelId;
            car.TrimId ??= res.TrimId;
        }

        car.VinDecoded = true;
        car.VinDecodedAtUtc = DateTime.UtcNow;
        car.UpdatedAt = DateTime.UtcNow;
        logger.LogDebug("Applied vPIC decode to car {Id} ({Make} {Model})", car.Id, r.Make, r.Model);
    }
}
