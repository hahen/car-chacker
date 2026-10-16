namespace CarPrices.Scraping.Copart;

/// <summary>Configuration for the Copart collector (bound from the "Copart" config section).</summary>
public class CopartOptions
{
    public bool Headless { get; set; } = true;
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36";
    public string BaseUrl { get; set; } = "https://www.copart.com";
    public string SearchPath { get; set; } = "/public/lots/search-results";

    public int PageSize { get; set; } = 100;
    /// <summary>Copart caps pagination at ~50 pages per query.</summary>
    public int MaxPages { get; set; } = 50;

    public int MinDelayMs { get; set; } = 1500;
    public int MaxDelayMs { get; set; } = 4000;
    public int NavTimeoutMs { get; set; } = 45000;

    /// <summary>Optional proxy (e.g. a residential proxy) as host:port or scheme://host:port.</summary>
    public string? ProxyServer { get; set; }

    /// <summary>Persistent browser profile dir (keeps Incapsula cookies between runs). Defaults under data/.</summary>
    public string? UserDataDir { get; set; }
}
