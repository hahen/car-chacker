using CarPrices.Scraping.History;

namespace CarPrices.Tests;

public class CarsBidsHistoryParserTests
{
    [Fact]
    public void ParseIdentity_from_url()
    {
        var id = CarsBidsHistoryParser.ParseIdentity(
            "/make/230-MERCEDES_BENZ/105116-AMG_G_63/2021_MERCEDES_BENZ_AMG_G_63_45117468_W1NYC7HJ2MX405010");
        Assert.Equal(2021, id.Year);
        Assert.Equal("MERCEDES BENZ", id.Make);
        Assert.Equal("AMG G 63", id.Model);
        Assert.Equal("W1NYC7HJ2MX405010", id.Vin);
    }

    [Fact]
    public void FindModels_matches_tokens()
    {
        const string html = """
            <a href="/make/46-BMW/111-328/page-1">328</a>
            <a href="/make/46-BMW/51381-1_SERIES/page-1">1 SERIES</a>
            <a href="/make/46-BMW/999-3_SERIES/page-1">3 SERIES</a>
            """;
        var models = CarsBidsHistoryParser.FindModels(html, "46", new[] { "3", "SERIES" });
        Assert.Single(models);
        Assert.Equal("999", models[0].Id);
        Assert.Equal("3_SERIES", models[0].Name);
    }

    [Fact]
    public void ParseListingDetailUrls_extracts_cars()
    {
        const string html = """
            <a href="/make/46-BMW/999-3_SERIES/2021_BMW_3_SERIES_45117468_WBA5R1C0XLFH12345">x</a>
            <a href="/make/46-BMW/999-3_SERIES/2020_BMW_3_SERIES_44001122_WBA5R1C01LFH54321">y</a>
            <a href="/about">no</a>
            """;
        var urls = CarsBidsHistoryParser.ParseListingDetailUrls(html);
        Assert.Equal(2, urls.Count);
        Assert.All(urls, u => Assert.Contains("3_SERIES", u));
    }

    [Fact]
    public void ParseDetail_emits_sold_rows_only()
    {
        const string html = """
            <div>1. Lot: 45117468</div>
            <table class="table table-details">
              <tr><td>Doc Type</td><td>Clear</td></tr>
              <tr><td>Odometer:</td><td>113769.0(Actual)</td></tr>
              <tr><td>Primary Damage</td><td>Mechanical</td></tr>
              <tr><td>Color</td><td>White</td></tr>
              <tr><td>Fuel</td><td>Gasoline</td></tr>
              <tr><td>VIN</td><td>W1NYC7HJ2MX405010</td></tr>
            </table>
            <table class="table auction-table">
              <tr><th>N</th><th>Date</th><th>Auction</th><th>Participants</th><th>Auction Type</th><th>Last Bid</th><th>Sold Status</th><th>Buyer Location</th></tr>
              <tr><td>1</td><td>2024-06-03 13:30:00.0</td><td>Manchester</td><td></td><td></td><td>0.0</td><td></td><td>Montana</td></tr>
              <tr><td>2</td><td>2024-07-01 13:30:00.0</td><td>Manchester</td><td></td><td></td><td>56000.0</td><td>Sold on Approval</td><td>Georgia</td></tr>
            </table>
            <img src="https://cdn4.carsbidshistory.com/photo2/45117468/W1NYC7HJ2MX405010_1.jpg">
            """;
        var id = new CbhIdentity(2021, "BMW", "3 Series", "330i", "W1NYC7HJ2MX405010", "http://x");
        var lots = CarsBidsHistoryParser.ParseDetail(html, id);

        var lot = Assert.Single(lots); // only the sold row (the 0.0/blank row is skipped)
        Assert.Equal("45117468", lot.LotNumber);
        Assert.True(lot.IsSold);
        Assert.Equal(56000m, lot.SalePrice);
        Assert.Equal("W1NYC7HJ2MX405010", lot.Vin);
        Assert.Equal(2021, lot.Year);
        Assert.Equal("3 Series", lot.Model);
        Assert.Equal("330i", lot.Trim);
        Assert.Equal(113769, lot.Odometer);
        Assert.Equal("Mechanical", lot.PrimaryDamage);
        Assert.Equal("Clear", lot.TitleDescription);
        Assert.Equal("Gasoline", lot.FuelType);
        Assert.Equal(new DateTime(2024, 7, 1, 13, 30, 0, DateTimeKind.Utc), lot.AuctionDateUtc);
        Assert.Contains(lot.ImageUrls, u => u.Contains("/photo2/45117468/"));
    }
}
