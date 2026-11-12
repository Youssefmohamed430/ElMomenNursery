using ElMomenNursery.Infrastructure.Configuration;
using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddNurseryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(LocalDataOptions.SectionName).Get<LocalDataOptions>() ?? new LocalDataOptions();
        var dataDirectory = string.IsNullOrWhiteSpace(options.DirectoryPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlMoumenNursery")
            : Path.GetFullPath(Environment.ExpandEnvironmentVariables(options.DirectoryPath));
        Directory.CreateDirectory(dataDirectory);
        var connectionString = configuration.GetConnectionString("Nursery");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var databasePath = Path.Combine(dataDirectory, "nursery.db");
            connectionString = $"Data Source={databasePath};Foreign Keys=True;Default Timeout=30";
        }
        else
        {
            connectionString = Environment.ExpandEnvironmentVariables(connectionString);
        }

        services.AddDbContext<NurseryDbContext>(db => db.UseSqlite(
            connectionString,
            sqlite => sqlite.MigrationsAssembly(typeof(NurseryDbContext).Assembly.FullName)));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        return services;
    }
}
