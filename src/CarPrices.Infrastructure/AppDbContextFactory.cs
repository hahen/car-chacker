using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarPrices.Infrastructure;

/// <summary>Design-time factory so `dotnet ef` can build the context without a startup project.</summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(CarDbPaths.ConnectionString())
            .Options;
        return new AppDbContext(options);
    }
}
