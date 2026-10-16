namespace CarPrices.Enrichment.Vpic;

public class VpicOptions
{
    public string BaseUrl { get; set; } = "https://vpic.nhtsa.dot.gov/api";
    public int BatchSize { get; set; } = 50;
}
