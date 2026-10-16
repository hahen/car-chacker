using Microsoft.Extensions.DependencyInjection;

namespace CarPrices.Normalization;

public static class DependencyInjection
{
    public static IServiceCollection AddNormalization(this IServiceCollection services)
    {
        services.AddScoped<TaxonomyResolver>();
        return services;
    }
}
