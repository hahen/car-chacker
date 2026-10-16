using CarPrices.Enrichment.Features;
using CarPrices.Enrichment.Vpic;
using CarPrices.Normalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarPrices.Enrichment;

public static class DependencyInjection
{
    public static IServiceCollection AddEnrichment(this IServiceCollection services, IConfiguration? config = null)
    {
        services.AddHttpClient<VpicClient>(c => c.Timeout = TimeSpan.FromSeconds(60));
        services.AddNormalization();
        services.AddScoped<VinDecodeService>();
        services.AddScoped<FeatureExtractor>();
        services.AddScoped<EnrichmentService>();

        if (config is not null)
            services.Configure<VpicOptions>(config.GetSection("Vpic"));
        else
            services.AddOptions<VpicOptions>();

        return services;
    }
}
