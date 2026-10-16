using CarPrices.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CarPrices.Tests;

/// <summary>An isolated in-memory SQLite database (schema via EnsureCreated, seeded lookups).</summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _conn;
    public AppDbContext Db { get; }

    public TestDb(bool seed = true)
    {
        _conn = new SqliteConnection("Data Source=:memory:");
        _conn.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();
        if (seed) DbInitializer.Seed(Db);
    }

    public void Dispose()
    {
        Db.Dispose();
        _conn.Dispose();
    }
}
