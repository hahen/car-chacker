using CarPrices.Normalization;
using CarPrices.Scraping.Collection;
using CarPrices.Scraping.Copart;
using CarPrices.Scraping.History;
using CarPrices.Scraping.Iaai;
using CarPrices.Scraping.Ingestion;
using CarPrices.Scraping.VinCheck;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarPrices.Scraping;

public static class DependencyInjection
{
    public static IServiceCollection AddScraping(this IServiceCollection services, IConfiguration? config = null)
    {
        services.AddHttpClient<VinCheckImporter>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(60);
            c.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) CarPrices/0.1");
        });

        services.AddNormalization();
        services.AddScoped<LotIngestionService>();
        services.AddScoped<CollectionTargetService>();
        services.AddScoped<CopartCollector>();
        services.AddScoped<IaaiCollector>();
        services.AddScoped<CarsBidsHistoryCollector>();

        services.AddHttpClient<CarsBidsHistoryClient>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(30);
            c.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
            c.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            c.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        });

        if (config is not null)
        {
            services.Configure<CopartOptions>(config.GetSection("Copart"));
            services.Configure<IaaiOptions>(config.GetSection("Iaai"));
            services.Configure<HistoryOptions>(config.GetSection("History"));
            services.Configure<CollectionOptions>(config.GetSection("Collection"));
        }
        else
        {
            services.AddOptions<CopartOptions>();
            services.AddOptions<IaaiOptions>();
            services.AddOptions<HistoryOptions>();
            services.AddOptions<CollectionOptions>();
        }

        return services;
    }
}
