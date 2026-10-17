namespace CarPrices.Scraping;

/// <summary>Outcome of a collector run (discovery or close-capture).</summary>
public record ScrapeRunSummary(long RunId, int LotsSeen, int New, int Updated, int PricesCaptured, int Errors);
