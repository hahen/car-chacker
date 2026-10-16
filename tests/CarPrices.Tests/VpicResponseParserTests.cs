using CarPrices.Enrichment.Vpic;

namespace CarPrices.Tests;

public class VpicResponseParserTests
{
    private const string Sample = """
    {
      "Count": 1,
      "Message": "Results returned successfully",
      "Results": [
        {
          "VIN": "1HGCM82633A004352",
          "Make": "HONDA",
          "Model": "Accord",
          "ModelYear": "2003",
          "Trim": "EX",
          "BodyClass": "Sedan/Saloon",
          "DriveType": "FWD/Front-Wheel Drive",
          "FuelTypePrimary": "Gasoline",
          "EngineCylinders": "4",
          "AdaptiveCruiseControl": "",
          "KeylessIgnition": "Standard",
          "ErrorCode": "0"
        }
      ]
    }
    """;

    [Fact]
    public void Parses_fields_and_presence()
    {
        var r = Assert.Single(VpicResponseParser.Parse(Sample));
        Assert.Equal("HONDA", r.Make);
        Assert.Equal("Accord", r.Model);
        Assert.Equal(2003, r.ModelYear);
        Assert.Equal("EX", r.Trim);
        Assert.Equal(4, r.EngineCylinders);
        Assert.Contains("FWD", r.DriveType);
        Assert.True(r.DecodedOk);
        Assert.False(r.IsPresent("AdaptiveCruiseControl")); // blank
        Assert.True(r.IsPresent("KeylessIgnition"));
    }
}
