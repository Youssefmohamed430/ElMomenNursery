using ElMomenNursery.Infrastructure.Configuration;
using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Infrastructure.Security;
using ElMomenNursery.Infrastructure.Spreadsheet;
using ElMomenNursery.Infrastructure.Documents;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.Sqlite;
using Serilog;

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

        var sqliteConnection = new SqliteConnectionStringBuilder(connectionString);
        if (!string.IsNullOrWhiteSpace(sqliteConnection.DataSource) && sqliteConnection.DataSource != ":memory:")
        {
            var databasePath = Path.GetFullPath(sqliteConnection.DataSource);
            var databaseDirectory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrWhiteSpace(databaseDirectory) && !Directory.Exists(databaseDirectory))
            {
                try
                {
                    Directory.CreateDirectory(databaseDirectory);
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Could not create directory for configured database path");
                    throw new InvalidOperationException("The configured database directory could not be created.", exception);
                }
            }

            Log.Information("Resolved nursery data directory; dataDirectory={DataDirectory}; databasePath={DatabasePath}; databaseExists={DatabaseExists}",
                dataDirectory, databasePath, File.Exists(databasePath));
        }
        else
        {
            Log.Information("Resolved nursery data directory; dataDirectory={DataDirectory}; databaseExists={DatabaseExists}",
                dataDirectory, false);
        }

        services.AddSingleton<SoftDeleteInterceptor>();
        services.AddDbContext<NurseryDbContext>((provider, db) => db
            .AddInterceptors(provider.GetRequiredService<SoftDeleteInterceptor>())
            .UseSqlite(
                connectionString,
                sqlite => sqlite.MigrationsAssembly(typeof(NurseryDbContext).Assembly.FullName))
            .ConfigureWarnings(warnings => warnings
                .Ignore(RelationalEventId.PendingModelChangesWarning)));
        services.AddSingleton<IDatabaseBackupService>(provider => new SqliteDatabaseBackupService(
            connectionString, provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SqliteDatabaseBackupService>>()));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.Configure<AuthSecurityOptions>(options =>
        {
            var auth = configuration.GetSection(AuthSecurityOptions.SectionName);
            options.MaxFailedAttempts = auth.GetValue(nameof(AuthSecurityOptions.MaxFailedAttempts), options.MaxFailedAttempts);
            options.LockoutMinutes = auth.GetValue(nameof(AuthSecurityOptions.LockoutMinutes), options.LockoutMinutes);
        });
        services.AddScoped<IAuthService, LocalAuthService>();
        services.AddScoped<IPermissionService, LocalPermissionService>();
        services.AddScoped<IAccountManagementService, LocalAccountManagementService>();
        services.AddSingleton<IExcelExportService, ExcelExportService>();
        QuestPDF.Settings.License = LicenseType.Community;
        var printerOptions = configuration.GetSection(PrinterOptions.SectionName).Get<PrinterOptions>() ?? new PrinterOptions();
        services.AddSingleton<IReceiptPdfService>(_ => new ReceiptPdfService(dataDirectory, printerOptions));
        services.AddSingleton<IInvoicePdfService>(sp => new InvoicePdfService(dataDirectory, sp.GetRequiredService<IServiceScopeFactory>()));
        if (OperatingSystem.IsWindows())
        {
#pragma warning disable CA1416
            services.AddSingleton<IReceiptPrinterService>(_ => new ReceiptPrinterService(printerOptions));
#pragma warning restore CA1416
        }
        else
        {
            services.AddSingleton<IReceiptPrinterService, UnsupportedReceiptPrinterService>();
        }
        services.AddSingleton<ISessionManager, SessionManager>();
        return services;
    }
}

internal sealed class UnsupportedReceiptPrinterService : IReceiptPrinterService
{
    public Task<ElMomenNursery.Domain.Abstractions.Result> PrintReceiptAsync(
        ElMomenNursery.Application.DTOs.PaymentReceiptDto receipt, bool isCopy, CancellationToken cancellationToken = default) =>
        Task.FromResult(ElMomenNursery.Domain.Abstractions.Result.Failure(
            new ElMomenNursery.Domain.Abstractions.Error("Receipts.WindowsPrintingOnly", "طباعة الإيصالات متاحة على Windows فقط.")));
}
