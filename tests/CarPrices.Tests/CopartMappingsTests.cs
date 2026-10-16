using CarPrices.Domain.Enums;
using CarPrices.Scraping.Ingestion;

namespace CarPrices.Tests;

public class CopartMappingsTests
{
    [Theory]
    [InlineData("A", OdometerBrand.Actual)]
    [InlineData("N", OdometerBrand.NotActual)]
    [InlineData("E", OdometerBrand.Exempt)]
    [InlineData("X", OdometerBrand.ExceedsMechanicalLimit)]
    [InlineData(null, OdometerBrand.Unknown)]
    public void OdometerBrand_maps(string? obc, OdometerBrand expected) =>
        Assert.Equal(expected, CopartMappings.OdometerBrandFrom(obc));

    [Theory]
    [InlineData("TITLEGROUP_S", null, TitleGroup.Salvage)]
    [InlineData("TITLEGROUP_C", null, TitleGroup.Clean)]
    [InlineData(null, "SALVAGE CERTIFICATE", TitleGroup.Salvage)]
    [InlineData(null, "CLEAN", TitleGroup.Clean)]
    [InlineData(null, "REBUILT", TitleGroup.Rebuilt)]
    public void TitleGroup_maps(string? tgc, string? td, TitleGroup expected) =>
        Assert.Equal(expected, CopartMappings.TitleGroupFrom(tgc, td));

    [Theory]
    [InlineData("FRONT END", "FRONT")]
    [InlineData("REAR END", "REAR")]
    [InlineData("SIDE", "SIDE")]
    [InlineData("WATER/FLOOD", "WATER")]
    [InlineData("ALL OVER", "ALL_OVER")]
    public void Damage_maps(string text, string expected) =>
        Assert.Equal(expected, CopartMappings.DamageCodeFrom(text));

    [Fact]
    public void ClassifyVin_masked_yields_pattern_only()
    {
        var (full, pattern) = CopartMappings.ClassifyVin("KMHD84LF5HU******");
        Assert.Null(full);
        Assert.Equal("KMHD84LF5HU", pattern);
    }

    [Fact]
    public void ClassifyVin_full_yields_full_and_pattern()
    {
        var (full, pattern) = CopartMappings.ClassifyVin("1HGCM82633A004352");
        Assert.Equal("1HGCM82633A004352", full);
        Assert.Equal("1HGCM82633A", pattern);
    }

    [Fact]
    public void DiscoverySaleStatus_future_is_upcoming()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(SaleStatus.Upcoming, CopartMappings.DiscoverySaleStatus(now.AddDays(5), now));
        Assert.Equal(SaleStatus.Unknown, CopartMappings.DiscoverySaleStatus(now.AddDays(-5), now));
        Assert.Equal(SaleStatus.Unknown, CopartMappings.DiscoverySaleStatus(null, now));
    }
}
