using CarPrices.Domain.Enums;
using CarPrices.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarPrices.Statistics;

/// <summary>Browse/filter the collected inventory (lots + cars + photos + features).</summary>
public class CatalogService(AppDbContext db)
{
    public async Task<List<CatalogFacetDto>> GetMakesAsync(CancellationToken ct = default)
    {
        var names = await db.Cars.Where(c => c.MakerId != null).Select(c => c.Maker!.Name).ToListAsync(ct);
        return Facets(names);
    }

    public async Task<List<CatalogFacetDto>> GetModelsAsync(string make, CancellationToken ct = default)
    {
        var lower = make.ToLower();
        var names = await db.Cars
            .Where(c => c.MakerId != null && c.ModelId != null && c.Maker!.Name.ToLower() == lower)
            .Select(c => c.Model!.Name).ToListAsync(ct);
        return Facets(names);
    }

    private static List<CatalogFacetDto> Facets(IEnumerable<string> names) =>
        names.GroupBy(n => n)
            .Select(g => new CatalogFacetDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count).ThenBy(x => x.Name)
            .ToList();

    public async Task<LotSearchResult> SearchLotsAsync(LotFilter f, CancellationToken ct = default)
    {
        var q = db.Lots
            .Include(l => l.Car).ThenInclude(c => c!.Maker)
            .Include(l => l.Car).ThenInclude(c => c!.Model)
            .Include(l => l.Car).ThenInclude(c => c!.Trim)
            .Include(l => l.TitleType)
            .Include(l => l.PrimaryDamage)
            .Where(l => l.Car != null);

        if (!string.IsNullOrWhiteSpace(f.Make)) { var m = f.Make.ToLower(); q = q.Where(l => l.Car!.Maker!.Name.ToLower() == m); }
        if (!string.IsNullOrWhiteSpace(f.Model)) { var m = f.Model.ToLower(); q = q.Where(l => l.Car!.Model!.Name.ToLower() == m); }
        if (!string.IsNullOrWhiteSpace(f.Source) && Enum.TryParse<Source>(f.Source, true, out var src)) q = q.Where(l => l.Source == src);
        if (f.YearFrom is int yf) q = q.Where(l => l.Car!.ModelYear >= yf);
        if (f.YearTo is int yt) q = q.Where(l => l.Car!.ModelYear <= yt);
        if (!string.IsNullOrWhiteSpace(f.TitleGroup) && Enum.TryParse<TitleGroup>(f.TitleGroup, true, out var tg)) q = q.Where(l => l.TitleType != null && l.TitleType.Group == tg);
        if (!string.IsNullOrWhiteSpace(f.DamageGroup) && Enum.TryParse<DamageGroup>(f.DamageGroup, true, out var dg)) q = q.Where(l => l.PrimaryDamage != null && l.PrimaryDamage.Group == dg);
        if (f.MinPrice is decimal mn) q = q.Where(l => (l.SalePrice ?? l.CurrentBid ?? l.BuyNowPrice) >= mn);
        if (f.MaxPrice is decimal mx) q = q.Where(l => (l.SalePrice ?? l.CurrentBid ?? l.BuyNowPrice) <= mx);
        if (f.MaxOdometer is int od) q = q.Where(l => l.Odometer != null && l.Odometer <= od);
        if (f.HasVin == true) q = q.Where(l => l.Car!.Vin != null);
        if (string.Equals(f.Status, "sold", StringComparison.OrdinalIgnoreCase)) q = q.Where(l => l.IsSold);
        else if (string.Equals(f.Status, "upcoming", StringComparison.OrdinalIgnoreCase)) q = q.Where(l => !l.IsSold);

        var total = await q.CountAsync(ct);

        q = f.Sort switch
        {
            "auction" => q.OrderBy(l => l.AuctionDateUtc),
            "price" => q.OrderByDescending(l => l.SalePrice ?? l.CurrentBid ?? l.BuyNowPrice),
            "year" => q.OrderByDescending(l => l.Car!.ModelYear),
            _ => q.OrderByDescending(l => l.Id),
        };

        var page = Math.Max(1, f.Page);
        var size = Math.Clamp(f.PageSize, 1, 100);
        var lots = await q.Skip((page - 1) * size).Take(size).ToListAsync(ct);

        var lotIds = lots.Select(l => l.Id).ToList();
        var carIds = lots.Select(l => l.CarId).Distinct().ToList();

        var thumbs = (await db.LotImages.Where(i => lotIds.Contains(i.LotId)).OrderBy(i => i.Sequence).ToListAsync(ct))
            .GroupBy(i => i.LotId).ToDictionary(g => g.Key, g => g.First().Url);

        var feats = (await db.CarFeatures.Where(cf => carIds.Contains(cf.CarId))
                .Join(db.Features, cf => cf.FeatureId, fe => fe.Id, (cf, fe) => new { cf.CarId, fe.Code }).ToListAsync(ct))
            .GroupBy(x => x.CarId).ToDictionary(g => g.Key, g => g.Select(x => x.Code).OrderBy(c => c).ToList());

        var items = lots.Select(l => new LotListItemDto(
            l.Id, l.Source.ToString(), l.SourceLotNumber, l.LotUrl,
            l.Car!.Maker?.Name, l.Car.Model?.Name, l.Car.Trim?.Name, l.Car.ModelYear,
            l.Car.Vin ?? l.Car.VinPattern, l.Car.Vin != null,
            l.CurrentBid, l.BuyNowPrice, l.SalePrice, l.IsSold, l.SaleStatus.ToString(),
            l.TitleType?.Code, l.PrimaryDamage?.Code, l.Odometer,
            l.LocationCity, l.LocationState, l.AuctionDateUtc,
            thumbs.GetValueOrDefault(l.Id),
            feats.TryGetValue(l.CarId, out var fc) ? fc : new List<string>())).ToList();

        return new LotSearchResult(total, page, size, items);
    }
}
