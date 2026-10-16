using CarPrices.Domain.Entities;
using CarPrices.Domain.Enums;
using CarPrices.Infrastructure;
using CarPrices.Normalization;
using CarPrices.Scraping.Copart;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarPrices.Scraping.Ingestion;

public record IngestOutcome(bool IsNew, long CarId, long LotId);

/// <summary>
/// Upserts a <see cref="ScrapedLot"/> into Cars + Lots (+ RawPayload), resolving make/model/trim
/// via the taxonomy and title/damage via the lookup tables. Idempotent on
/// (Source, SourceLotNumber, AuctionDate): re-ingesting the same lot updates it in place.
/// </summary>
public class LotIngestionService(AppDbContext db, TaxonomyResolver taxonomy, ILogger<LotIngestionService> logger)
{
    private Dictionary<string, long>? _titleTypes;
    private Dictionary<string, long>? _damageTypes;

    public async Task<IngestOutcome> IngestAsync(ScrapedLot s, Source source, long? scrapeRunId, CancellationToken ct = default)
    {
        await EnsureLookupsAsync(ct);
        var now = DateTime.UtcNow;

        var (fullVin, pattern) = CopartMappings.ClassifyVin(s.Vin);
        var res = await taxonomy.ResolveAsync(s.Make, s.Model, s.Trim, autoCreate: true, ct);

        // Find an existing lot (same source + number + auction date).
        var baseQuery = db.Lots.Where(l => l.Source == source && l.SourceLotNumber == s.LotNumber);
        var lot = s.AuctionDateUtc is null
            ? await baseQuery.FirstOrDefaultAsync(l => l.AuctionDateUtc == null, ct)
            : await baseQuery.FirstOrDefaultAsync(l => l.AuctionDateUtc == s.AuctionDateUtc, ct);
        var newLot = lot is null;

        // Resolve the physical car.
        Car car;
        if (lot is not null)
            car = await db.Cars.FirstAsync(c => c.Id == lot.CarId, ct);
        else if (fullVin is not null)
        {
            car = await db.Cars.FirstOrDefaultAsync(c => c.Vin == fullVin, ct) ?? new Car();
            if (car.Id == 0) db.Cars.Add(car);
        }
        else
        {
            car = new Car();
            db.Cars.Add(car);
        }

        if (fullVin is not null)
        {
            car.Vin = fullVin;
            if (car.VinSource == VinSource.Unknown) car.VinSource = VinSource.TextField;
        }
        car.VinPattern ??= pattern;
        car.MakerId ??= res.MakerId;
        car.ModelId ??= res.ModelId;
        car.TrimId ??= res.TrimId;
        car.ModelYear ??= s.Year;
        car.ExteriorColor ??= s.Color;
        car.DriveType ??= s.DriveType;
        car.FuelType ??= s.FuelType;
        car.EngineDescription ??= s.EngineDescription;
        car.UpdatedAt = now;

        await db.SaveChangesAsync(ct); // assign car.Id for new cars

        // Raw payload (one per lot, updated in place).
        RawPayload raw;
        if (lot?.RawPayloadId is not null)
        {
            raw = await db.RawPayloads.FirstAsync(p => p.Id == lot.RawPayloadId, ct);
            raw.Content = s.RawJson;
            raw.CapturedAtUtc = now;
        }
        else
        {
            raw = new RawPayload
            {
                Source = source,
                SourceLotNumber = s.LotNumber,
                Content = s.RawJson,
                CapturedAtUtc = now,
            };
            db.RawPayloads.Add(raw);
        }

        lot ??= new Lot { Source = source, SourceLotNumber = s.LotNumber, FirstSeenUtc = now };
        if (newLot) db.Lots.Add(lot);

        lot.CarId = car.Id;
        lot.AuctionDateUtc = s.AuctionDateUtc;
        lot.CurrentBid = s.CurrentBid;
        lot.BuyNowPrice = s.BuyNowPrice;
        lot.EstimatedRetailValue = s.EstimatedRetailValue;
        lot.Currency = s.Currency ?? lot.Currency;
        lot.Odometer = s.Odometer;
        lot.OdometerBrand = CopartMappings.OdometerBrandFrom(s.OdometerBrandCode);
        lot.YardCode = s.YardName;
        lot.LocationCity = s.LocationCity;
        lot.LocationState = s.LocationState;
        lot.LocationCountry = s.LocationCountry;
        lot.HasKeys = s.HasKeys;
        lot.SaleStatusRaw = $"ss={s.SaleStatusCode};lsts={s.LotStatus}";
        if (lot.SaleStatus is SaleStatus.Unknown or SaleStatus.Upcoming or SaleStatus.Live)
            lot.SaleStatus = CopartMappings.DiscoverySaleStatus(s.AuctionDateUtc, now);
        lot.TitleTypeId = ResolveTitle(s);
        lot.PrimaryDamageId = ResolveDamage(s.PrimaryDamage);
        lot.SecondaryDamageId = ResolveDamage(s.SecondaryDamage);
        lot.LastSeenUtc = now;
        lot.RawPayload = raw;

        await db.SaveChangesAsync(ct);
        logger.LogDebug("Ingested lot {Lot} ({State}) -> car {Car}", s.LotNumber, newLot ? "new" : "updated", car.Id);
        return new IngestOutcome(newLot, car.Id, lot.Id);
    }

    private long? ResolveTitle(ScrapedLot s)
    {
        var group = CopartMappings.TitleGroupFrom(s.TitleGroupCode, s.TitleDescription);
        var code = CopartMappings.TitleTypeCodeFor(group);
        return code is not null && _titleTypes!.TryGetValue(code, out var id) ? id : null;
    }

    private long? ResolveDamage(string? text)
    {
        var code = CopartMappings.DamageCodeFrom(text);
        return code is not null && _damageTypes!.TryGetValue(code, out var id) ? id : null;
    }

    private async Task EnsureLookupsAsync(CancellationToken ct)
    {
        _titleTypes ??= await db.TitleTypes.ToDictionaryAsync(t => t.Code, t => t.Id, ct);
        _damageTypes ??= await db.DamageTypes.ToDictionaryAsync(d => d.Code, d => d.Id, ct);
    }
}
