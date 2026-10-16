using CarPrices.Scraping.VinCheck;
using Microsoft.Extensions.DependencyInjection;

namespace CarPrices.Scraping;

public static class DependencyInjection
{
    public static IServiceCollection AddScraping(this IServiceCollection services)
    {
        services.AddHttpClient<VinCheckImporter>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(60);
            c.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) CarPrices/0.1");
        });
        return services;
    }
}
