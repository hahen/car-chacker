using CarPrices.Scraping.Copart;

namespace CarPrices.Tests;

public class CopartLotParserTests
{
    private const string Sample = """
    {
      "returnCode": 1,
      "data": {
        "results": {
          "content": [
            {
              "lotNumberStr": "35272471",
              "ln": 35272471,
              "fv": "KMHD84LF5HU******",
              "mkn": "HYUNDAI",
              "lmg": "ELANTRA",
              "lm": "ELANTRA",
              "ltd": "SE",
              "lcy": 2017,
              "hb": 225,
              "bnp": 0,
              "ad": 1792080000000,
              "lad": 1605081600000,
              "ss": 3,
              "lsts": "O",
              "yn": "IL - CHICAGO NORTH",
              "locCity": "CHICAGO",
              "locState": "IL",
              "locCountry": "USA",
              "orr": 94023,
              "obc": "A",
              "clr": "RED",
              "drv": "Front-wheel Drive",
              "ft": "GAS",
              "td": "SALVAGE CERTIFICATE",
              "tgc": "TITLEGROUP_S",
              "dd": "FRONT END",
              "sdd": "SIDE",
              "hk": "YES",
              "cuc": "USD"
            }
          ]
        }
      }
    }
    """;

    [Fact]
    public void Parses_lot_fields()
    {
        var lots = CopartLotParser.ParseResponse(Sample);
        var lot = Assert.Single(lots);

        Assert.Equal("35272471", lot.LotNumber);
        Assert.Equal("KMHD84LF5HU******", lot.Vin);
        Assert.Equal("HYUNDAI", lot.Make);
        Assert.Equal("ELANTRA", lot.Model);
        Assert.Equal("SE", lot.Trim);
        Assert.Equal(2017, lot.Year);
        Assert.Equal(225m, lot.CurrentBid);
        Assert.Null(lot.BuyNowPrice); // 0 -> null
        Assert.Equal(94023, lot.Odometer);
        Assert.Equal("A", lot.OdometerBrandCode);
        Assert.Equal("RED", lot.Color);
        Assert.Equal("FRONT END", lot.PrimaryDamage);
        Assert.Equal("SIDE", lot.SecondaryDamage);
        Assert.Equal("TITLEGROUP_S", lot.TitleGroupCode);
        Assert.Equal("IL", lot.LocationState);
        Assert.True(lot.HasKeys);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1792080000000).UtcDateTime, lot.AuctionDateUtc);
    }

    [Fact]
    public void Handles_results_array_shape()
    {
        const string json = """{"data":{"results":[{"lotNumberStr":"9","mkn":"FORD"}]}}""";
        var lots = CopartLotParser.ParseResponse(json);
        Assert.Single(lots);
        Assert.Equal("FORD", lots[0].Make);
    }

    [Fact]
    public void Empty_or_missing_results_returns_empty()
    {
        Assert.Empty(CopartLotParser.ParseResponse("""{"returnCode":1,"data":{}}"""));
    }
}
