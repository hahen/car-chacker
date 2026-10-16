using CarPrices.Enrichment.Features;
using CarPrices.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarPrices.Enrichment;

/// <summary>Finds cars needing enrichment, decodes their VIN (vPIC), and extracts baseline features.</summary>
public class EnrichmentService(
    AppDbContext db,
    VinDecodeService decode,
    FeatureExtractor features,
    ILogger<EnrichmentService> logger)
{
    public record EnrichSummary(int CarsProcessed, int Decoded, int FeaturesAdded, int Errors);

    public async Task<EnrichSummary> RunAsync(int maxCars = 200, CancellationToken ct = default)
    {
        var cars = await db.Cars
            .Where(c => !c.VinDecoded && (c.Vin != null || c.VinPattern != null))
            .OrderBy(c => c.Id)
            .Take(maxCars)
            .ToListAsync(ct);

        int decoded = 0, feat = 0, errors = 0;
        foreach (var car in cars)
        {
            try
            {
                var key = car.Vin ?? car.VinPattern!;
                var r = await decode.GetOrDecodeAsync(key, car.ModelYear, ct);
                if (r is null) continue;

                await decode.ApplyToCarAsync(car, r, ct);
                feat += await features.ExtractFromVpicAsync(car, r, ct);
                await db.SaveChangesAsync(ct);
                decoded++;
            }
            catch (Exception ex)
            {
                errors++;
                logger.LogError(ex, "Enrichment failed for car {Id}", car.Id);
            }
        }

        logger.LogInformation("Enrichment: {Cars} cars, {Decoded} decoded, {Feat} features, {Err} errors",
            cars.Count, decoded, feat, errors);
        return new EnrichSummary(cars.Count, decoded, feat, errors);
    }
}
