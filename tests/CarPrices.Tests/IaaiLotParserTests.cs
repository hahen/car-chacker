using CarPrices.Scraping.Iaai;

namespace CarPrices.Tests;

public class IaaiLotParserTests
{
    private const string Html = """
    <div class="table-row">
      <div class="table-row-inner table-row-header">
        <div class="table-cell">VEHICLE</div><div class="table-cell">CONDITION</div>
      </div>
      <div class="table-row-inner">
        <div class="table-cell table-cell--image">
          <a href="/VehicleDetail/46700483~US" aria-label="image">
            <img data-src="https://vis.iaai.com/resizer?imageKeys=46700483~SID~I1&width=400&height=300" />
          </a>
          <button type="button" onclick="ImageModalClicked('46200571', '46700483~US', 'WBADW7C52BE******', '714', '2011', 'BMW', '3 SERIES', '328I', 'false');">View All Images</button>
          <a href="JavaScript:void(0)" onclick="AddDelWatch(this, '46700483~US', '714','10/19/2026 8:30:00 AM +00:00','180616_2','Prebid/BuyNow'); return true;"></a>
        </div>
        <ul class="data-list data-list--search">
          <li class="data-list__item"><span class="data-list__value">Salvage</span></li>
          <li class="data-list__item"><span class="data-list__value">Front End</span></li>
          <li class="data-list__item"><span class="data-list__value">Collision</span></li>
        </ul>
        <ul class="data-list">
          <li class="data-list__item"><span class="data-list__value">127,478 mi</span></li>
          <li class="data-list__item"><span class="data-list__value">Run & Drive</span></li>
          <li class="data-list__item"><span class="data-list__value">Key Available</span></li>
        </ul>
        <ul class="data-list">
          <li class="data-list__item"><span class="data-list__value">3.0L I-6 DOHC, VVT, premium , 230HP</span></li>
          <li class="data-list__item"><span class="data-list__value">Gasoline</span></li>
          <li class="data-list__item"><span class="data-list__value">6 Cyl</span></li>
        </ul>
        <div class="branch">Charlotte (North Carolina)</div>
        <div class="acv">$7,004 USD</div>
        <div class="status">Pre-Bid OR Buy Now</div>
        <div class="buynow">$4,250 USD</div>
      </div>
    </div>
    """;

    [Fact]
    public void Parses_iaai_card()
    {
        var lots = IaaiLotParser.ParseSearchHtml(Html);
        var lot = Assert.Single(lots); // header row skipped (no ImageModalClicked)

        Assert.Equal("46700483~US", lot.LotNumber);
        Assert.Equal("WBADW7C52BE******", lot.Vin);
        Assert.Equal("BMW", lot.Make);
        Assert.Equal("3 SERIES", lot.Model);
        Assert.Equal("328I", lot.Trim);
        Assert.Equal(2011, lot.Year);
        Assert.Equal("714", lot.YardName);
        Assert.Equal(new DateTime(2026, 10, 19, 8, 30, 0, DateTimeKind.Utc), lot.AuctionDateUtc);
        Assert.Equal("Prebid/BuyNow", lot.LotStatus);
        Assert.Equal(127478, lot.Odometer);
        Assert.True(lot.HasKeys);
        Assert.Equal("Salvage", lot.TitleDescription);
        Assert.Equal("Front End", lot.PrimaryDamage);
        Assert.Equal("Gasoline", lot.FuelType);
        Assert.Equal("https://www.iaai.com/VehicleDetail/46700483~US", lot.LotUrl);
        Assert.Contains(lot.ImageUrls, u => u.Contains("vis.iaai.com/resizer"));
        Assert.Equal("Charlotte", lot.LocationCity);
        Assert.Equal("North Carolina", lot.LocationState);
        Assert.Equal(7004m, lot.CurrentBid);
        Assert.Equal(4250m, lot.BuyNowPrice);
    }

    [Theory]
    [InlineData("Winning Bid: $5,200 USD", true, 5200)]
    [InlineData("Final Sale Price $12,340", true, 12340)]
    [InlineData("Pre-Bid OR Buy Now $4,250", false, null)]
    public void Detects_sold(string text, bool expectedSold, int? expectedPrice)
    {
        var (sold, price) = IaaiCollector.DetectSold(text);
        Assert.Equal(expectedSold, sold);
        Assert.Equal(expectedPrice, price);
    }
}
