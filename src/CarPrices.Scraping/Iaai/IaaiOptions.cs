namespace CarPrices.Scraping.Iaai;

/// <summary>Configuration for the IAAI collector (bound from the "Iaai" config section).</summary>
public class IaaiOptions
{
    public bool Headless { get; set; } = true;
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36";
    public string BaseUrl { get; set; } = "https://www.iaai.com";

    /// <summary>IAAI search tends to lazy-load / rewrite the URL, so keep the page budget small.</summary>
    public int MaxPages { get; set; } = 3;

    public int MinDelayMs { get; set; } = 2000;
    public int MaxDelayMs { get; set; } = 5000;
    public int NavTimeoutMs { get; set; } = 45000;

    public string? ProxyServer { get; set; }
    public string? UserDataDir { get; set; }
}
