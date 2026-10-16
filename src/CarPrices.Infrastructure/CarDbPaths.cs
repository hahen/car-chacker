namespace CarPrices.Infrastructure;

/// <summary>Resolves stable local paths for the SQLite database and data files (repo-root/data).</summary>
public static class CarDbPaths
{
    /// <summary>Repo root (folder containing CarPrices.slnx/.sln), or the current directory.</summary>
    public static string RepoRoot() => FindRepoRoot() ?? Directory.GetCurrentDirectory();

    /// <summary>&lt;repo-root&gt;/data, created if missing.</summary>
    public static string DataDirectory()
    {
        var dir = Path.Combine(RepoRoot(), "data");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string DatabaseFilePath() => Path.Combine(DataDirectory(), "car.db");

    public static string ConnectionString() => $"Data Source={DatabaseFilePath()}";

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "CarPrices.slnx")) ||
                File.Exists(Path.Combine(dir.FullName, "CarPrices.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }
}
