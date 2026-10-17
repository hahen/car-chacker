namespace CarPrices.Scraping.History;

/// <summary>Configuration for the carsbidshistory.com archive scraper (bound from "History").</summary>
public class HistoryOptions
{
    public string BaseUrl { get; set; } = "https://carsbidshistory.com";
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36";

    public int MaxMakePages { get; set; } = 12;     // pages of the makes list to scan for an id
    public int MaxListingPages { get; set; } = 5;   // pages of a model's car list
    public int MaxCarsPerModel { get; set; } = 80;  // detail pages fetched per model
    public int MaxModelsPerTarget { get; set; } = 8; // matching sub-models (trims) per target

    public int MinDelayMs { get; set; } = 400;
    public int MaxDelayMs { get; set; } = 1100;
}
