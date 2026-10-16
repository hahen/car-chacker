using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarPrices.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the SQLite-backed AppDbContext. Pass null to use the repo-root data/car.db.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString = null)
    {
        var cs = string.IsNullOrWhiteSpace(connectionString) ? CarDbPaths.ConnectionString() : connectionString;
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(cs));
        return services;
    }
}
