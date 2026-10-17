using CarPrices.Domain.Entities;
using CarPrices.Domain.Enums;
using CarPrices.Statistics;
using Microsoft.EntityFrameworkCore;

namespace CarPrices.Tests;

public class CatalogServiceTests
{
    private static async Task SeedLotsAsync(TestDb t)
    {
        var maker = await t.Db.Makers.FirstAsync(m => m.NormalizedName == "bmw");
        var model = await t.Db.Models.FirstAsync(m => m.NormalizedName == "3 series");
        for (var i = 0; i < 5; i++)
        {
            var car = new Car { MakerId = maker.Id, ModelId = model.Id, ModelYear = 2019 + i, VinPattern = "WBA000000" + i };
            t.Db.Cars.Add(car);
            await t.Db.SaveChangesAsync();
            t.Db.Lots.Add(new Lot
            {
                CarId = car.Id,
                Source = Source.Iaai,
                SourceLotNumber = "L" + i,
                CurrentBid = 10000 + i * 1000,
                AuctionDateUtc = DateTime.UtcNow.AddDays(i + 1),
                SaleStatus = SaleStatus.Upcoming,
            });
            await t.Db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Lists_makes_with_counts()
    {
        using var t = new TestDb();
        await SeedLotsAsync(t);
        var makes = await new CatalogService(t.Db).GetMakesAsync();
        Assert.Contains(makes, x => x.Name == "BMW" && x.Count == 5);
    }

    [Fact]
    public async Task Searches_filters_and_pages()
    {
        using var t = new TestDb();
        await SeedLotsAsync(t);
        var svc = new CatalogService(t.Db);

        var all = await svc.SearchLotsAsync(new LotFilter(Make: "BMW"));
        Assert.Equal(5, all.Total);
        Assert.All(all.Items, i => Assert.Equal("BMW", i.Make));

        var filtered = await svc.SearchLotsAsync(new LotFilter(Make: "BMW", YearFrom: 2021));
        Assert.Equal(3, filtered.Total); // 2021, 2022, 2023

        var paged = await svc.SearchLotsAsync(new LotFilter(PageSize: 2));
        Assert.Equal(5, paged.Total);
        Assert.Equal(2, paged.Items.Count);
    }
}
