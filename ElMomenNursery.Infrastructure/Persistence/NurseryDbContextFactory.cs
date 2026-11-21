using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.Sqlite;

namespace ElMomenNursery.Infrastructure.Persistence;

public sealed class NurseryDbContextFactory : IDesignTimeDbContextFactory<NurseryDbContext>
{
    public NurseryDbContext CreateDbContext(string[] args)
    {
        var databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMoumenNursery",
            "nursery.db");
        var connectionString = $"Data Source={databasePath};Foreign Keys=True;Default Timeout=30";
        var sqliteConnection = new SqliteConnectionStringBuilder(connectionString);
        databasePath = sqliteConnection.DataSource;
        if (!string.Equals(databasePath, ":memory:", StringComparison.OrdinalIgnoreCase))
        {
            var fullPath = Path.GetFullPath(databasePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            sqliteConnection.DataSource = fullPath;
        }
        connectionString = sqliteConnection.ToString();

        var options = new DbContextOptionsBuilder<NurseryDbContext>()
            .UseSqlite(connectionString)
            .ConfigureWarnings(warnings => warnings
                .Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)
                .Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        return new NurseryDbContext(options);
    }
}
