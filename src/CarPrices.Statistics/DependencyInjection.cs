using Microsoft.Extensions.DependencyInjection;

namespace CarPrices.Statistics;

public static class DependencyInjection
{
    public static IServiceCollection AddStatistics(this IServiceCollection services)
    {
        services.AddScoped<StatisticsService>();
        return services;
    }
}
