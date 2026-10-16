using CarPrices.Domain.Entities;
using CarPrices.Domain.Enums;
using CarPrices.Domain.Vin;
using CarPrices.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarPrices.Vision;

/// <summary>
/// Runs VIN-from-image recovery for a lot: extracts candidates, logs every attempt to VinExtractions,
/// and (when a valid candidate matches the masked pattern) writes the full VIN back to the Car.
/// </summary>
public class VinImageRecoveryService(AppDbContext db, VinImageExtractor extractor, ILogger<VinImageRecoveryService> logger)
{
    public record RecoveryResult(bool Recovered, string? Vin, VinExtractionMethod? Method, double? Confidence, int Candidates);

    public async Task<RecoveryResult> ProcessLotImageAsync(long lotId, byte[] imageBytes, long? lotImageId = null, CancellationToken ct = default)
    {
        var lot = await db.Lots.Include(l => l.Car).FirstOrDefaultAsync(l => l.Id == lotId, ct);
        if (lot?.Car is null) throw new InvalidOperationException($"Lot {lotId} or its car was not found.");
        var car = lot.Car;
        var maskedVisible = car.VinPattern; // the known VIN prefix to cross-check against

        var candidates = extractor.ExtractCandidates(imageBytes);
        var best = extractor.BestCandidate(candidates, maskedVisible);

        foreach (var c in candidates)
        {
            db.VinExtractions.Add(new VinExtraction
            {
                LotId = lotId,
                LotImageId = lotImageId,
                Method = c.Method,
                Engine = c.Engine,
                RawText = c.Vin,
                CandidateVin = c.Vin,
                CheckDigitValid = c.CheckDigitValid,
                MatchesMaskedPattern = VinValidator.MatchesMaskedPattern(c.Vin, maskedVisible),
                Confidence = c.Confidence,
                Accepted = best is not null && ReferenceEquals(c, best),
            });
        }

        if (best is not null && car.Vin is null)
        {
            car.Vin = best.Vin;
            car.VinSource = best.Method == VinExtractionMethod.Barcode ? VinSource.PhotoBarcode : VinSource.PhotoOcr;
            car.VinConfidence = best.Confidence;
            car.VinPattern ??= best.Vin[..11];
            car.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("VIN-from-image lot {Lot}: {Count} candidate(s), recovered={Vin}",
            lotId, candidates.Count, best?.Vin ?? "(none)");
        return new RecoveryResult(best is not null, best?.Vin, best?.Method, best?.Confidence, candidates.Count);
    }
}
