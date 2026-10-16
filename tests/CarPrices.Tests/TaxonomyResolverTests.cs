using CarPrices.Normalization;
using Microsoft.EntityFrameworkCore;

namespace CarPrices.Tests;

public class TaxonomyResolverTests
{
    [Fact]
    public async Task Resolves_seeded_maker_by_alias()
    {
        using var t = new TestDb();
        var r = new TaxonomyResolver(t.Db);
        var id = await r.ResolveMakerAsync("HYUNDAI");
        Assert.NotNull(id);
        var maker = await t.Db.Makers.FindAsync(id!.Value);
        Assert.Equal("Hyundai", maker!.Name);
    }

    [Fact]
    public async Task Resolves_bmw_3series_and_trim()
    {
        using var t = new TestDb();
        var r = new TaxonomyResolver(t.Db);
        var res = await r.ResolveAsync("BMW", "3 Series", "330i");
        Assert.NotNull(res.MakerId);
        Assert.NotNull(res.ModelId);
        Assert.NotNull(res.TrimId);
    }

    [Fact]
    public async Task Resolves_model_by_alias_3er()
    {
        using var t = new TestDb();
        var r = new TaxonomyResolver(t.Db);
        var res = await r.ResolveAsync("BMW", "3er", null);
        Assert.NotNull(res.ModelId);
        var model = await t.Db.Models.FindAsync(res.ModelId!.Value);
        Assert.Equal("3 Series", model!.Name);
    }

    [Fact]
    public async Task Autocreates_unknown_maker_idempotently()
    {
        using var t = new TestDb();
        var r = new TaxonomyResolver(t.Db);
        var a = await r.ResolveMakerAsync("Rivian");
        var b = await r.ResolveMakerAsync("RIVIAN");
        Assert.NotNull(a);
        Assert.Equal(a, b);
        Assert.Equal(1, await t.Db.Makers.CountAsync(m => m.NormalizedName == "rivian"));
    }
}
