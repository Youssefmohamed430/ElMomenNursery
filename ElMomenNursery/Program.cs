using ElMomenNursery.Infrastructure;
using ElMomenNursery.Infrastructure.Persistence;
using ElMomenNursery.Forms;
using ElMomenNursery.Infrastructure.Security;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Context;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Sqlite;
using System.Reflection;

namespace ElMomenNursery;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            var baseConfiguration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
                .Build();
            ConfigureLogging(baseConfiguration);
            var configuration = new ConfigurationBuilder()
                .AddConfiguration(baseConfiguration)
                .AddInMemoryCollection(ResolveDatabaseProfile())
                .Build();
            Log.ForContext("AppVersion", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown")
                .Information("Application starting");

            ApplicationConfiguration.Initialize();
            System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            System.Windows.Forms.Application.ThreadException += (_, eventArgs) =>
            {
                Log.Error(eventArgs.Exception, "Unhandled Windows Forms exception");
                ShowUnexpectedError(WriteEmergencyStartupLog(eventArgs.Exception));
            };
            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            {
                if (eventArgs.ExceptionObject is Exception exception)
                {
                    Log.Fatal(exception, "Unhandled application-domain exception");
                }
            };

            var services = new ServiceCollection();
            services.AddLogging(logging =>
            {
                logging.AddSerilog(Log.Logger, dispose: false);
            });
            services.AddNurseryApplication();
            services.AddNurseryInfrastructure(configuration);
            using var serviceProvider = services.BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<NurseryDbContext>();
                var databasePath = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
                var pendingMigrations = db.Database.GetPendingMigrations().ToArray();
                Log.Information("Database startup check; databasePath={DatabasePath}; pendingMigrations={PendingMigrations}",
                    databasePath, pendingMigrations);
                if (pendingMigrations.Length > 0 && File.Exists(databasePath))
                    BackupDatabaseBeforeMigration(databasePath);

                var migrationTimer = Stopwatch.StartNew();
                try
                {
                    db.Database.Migrate();
                    Log.Information("Database migration completed; durationMs={DurationMs}", migrationTimer.ElapsedMilliseconds);
                }
                catch (Exception exception)
                {
                    Log.Fatal(exception, "Database migration failed; durationMs={DurationMs}", migrationTimer.ElapsedMilliseconds);
                    ShowStartupError();
                    return;
                }

                var seedTimer = Stopwatch.StartNew();
                try
                {
                    AuthSeed.InitializeAsync(db).GetAwaiter().GetResult();
                    LookupSeed.InitializeAsync(db).GetAwaiter().GetResult();
                    Log.Information("Authentication seed completed; durationMs={DurationMs}", seedTimer.ElapsedMilliseconds);
                }
                catch (Exception exception)
                {
                    Log.Fatal(exception, "Authentication seed failed; durationMs={DurationMs}", seedTimer.ElapsedMilliseconds);
                    ShowStartupError();
                    return;
                }
            }

            var session = serviceProvider.GetRequiredService<ISessionManager>();
            while (true)
            {
                using (var loginScope = serviceProvider.CreateScope())
                using (var login = new LoginForm(loginScope.ServiceProvider.GetRequiredService<IAuthService>(), IsMockupDatabaseSelected()))
                    if (login.ShowDialog() != DialogResult.OK) break;

                if (session.CurrentUser is null) break;
                var userId = session.CurrentUser.UserId;
                Log.Information("User logged in; UserId={UserId}", userId);

                using var appScope = serviceProvider.CreateScope();
                var auth = appScope.ServiceProvider.GetRequiredService<IAuthService>();
                using (LogContext.PushProperty("UserId", userId))
                {
                    System.Windows.Forms.Application.Run(new MainForm(session.CurrentUser, () => auth.LogoutAsync(),
                        appScope.ServiceProvider.GetRequiredService<IExcelExportService>(),
                        serviceProvider.GetRequiredService<IServiceScopeFactory>()));
                    if (session.IsAuthenticated)
                    {
                        try
                        {
                            auth.LogoutAsync().GetAwaiter().GetResult();
                        }
                        catch (Exception exception)
                        {
                            Log.Warning(exception, "Logout audit failed; UserId={UserId}", userId);
                        }
                    }
                }
                Log.Information("User logged out; UserId={UserId}", userId);
            }

            Log.Information("Application stopped normally");
        }
        catch (Exception exception)
        {
            var emergencyLogPath = WriteEmergencyStartupLog(exception);
            Log.Fatal(exception, "Application terminated during startup or execution");
            ShowUnexpectedError(emergencyLogPath);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static Dictionary<string, string?> ResolveDatabaseProfile()
    {
        var dataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlMoumenNursery");
        var isMockup = IsMockupDatabaseSelected();
        var databasePath = isMockup
            ? Path.Combine(dataRoot, "Mockup", "nursery.db")
            : Path.Combine(dataRoot, "nursery.db");
        if (isMockup)
            EnsureMockupDatabase(databasePath, Path.Combine(AppContext.BaseDirectory, "Mockup", "nursery-mockup.db"));

        return new Dictionary<string, string?>
        {
            ["LocalData:DirectoryPath"] = Path.GetDirectoryName(databasePath),
            ["ConnectionStrings:Nursery"] = $"Data Source={databasePath};Foreign Keys=True;Default Timeout=30"
        };
    }

    private static void EnsureMockupDatabase(string databasePath, string bundledTemplate)
    {
        if (!File.Exists(bundledTemplate))
        {
            Log.Warning("Bundled mockup database template is missing; path={TemplatePath}", bundledTemplate);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        if (!File.Exists(databasePath))
        {
            File.Copy(bundledTemplate, databasePath);
            Log.Information("Created mockup database from bundled sample data; path={DatabasePath}", databasePath);
            return;
        }

        if (!IsMockupDatabaseEmpty(databasePath)) return;

        var backupPath = $"{databasePath}.empty-backup-{DateTime.Now:yyyyMMdd-HHmmss}";
        File.Copy(databasePath, backupPath, overwrite: false);
        foreach (var suffix in new[] { "-wal", "-shm" })
        {
            var sidecar = databasePath + suffix;
            if (File.Exists(sidecar)) File.Copy(sidecar, backupPath + suffix, overwrite: false);
        }

        var replacementPath = databasePath + ".seed.tmp";
        File.Copy(bundledTemplate, replacementPath, overwrite: true);
        File.Move(replacementPath, databasePath, overwrite: true);
        foreach (var suffix in new[] { "-wal", "-shm" })
        {
            var sidecar = databasePath + suffix;
            if (File.Exists(sidecar)) File.Delete(sidecar);
        }

        Log.Information("Replaced empty mockup database with bundled sample data; path={DatabasePath}; backupPath={BackupPath}",
            databasePath, backupPath);
    }

    private static bool IsMockupDatabaseEmpty(string databasePath)
    {
        try
        {
            var builder = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadOnly };
            using var connection = new SqliteConnection(builder.ToString());
            connection.Open();
            var businessTables = new[]
            {
                "Children", "Employees", "Classrooms", "Subscriptions", "Payments", "Expenses",
                "InventoryItems", "Invoices", "ManualIncomes", "PayrollRecords", "ChildAttendances"
            };
            foreach (var table in businessTables)
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $table";
                command.Parameters.AddWithValue("$table", table);
                if (Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 0) continue;

                command.Parameters.Clear();
                command.CommandText = $"SELECT COUNT(*) FROM [{table}]";
                if (Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0) return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Could not read mockup database; treating it as empty for recovery; path={DatabasePath}", databasePath);
            return true;
        }
    }

    private static bool IsMockupDatabaseSelected()
    {
        var modeFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlMoumenNursery", "database-mode.txt");
        return string.Equals(File.Exists(modeFile) ? File.ReadAllText(modeFile).Trim() : "Production", "Mockup", StringComparison.OrdinalIgnoreCase);
    }

    private static void ConfigureLogging(IConfiguration configuration)
    {
        var configuredPath = configuration["Serilog:File:Path"] ?? "Logs/log-.txt";
        var localDataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlMoumenNursery");
        var logRoot = FindProjectRoot() ?? localDataRoot;
        var logPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(logRoot, configuredPath);
        logPath = Path.GetFullPath(logPath);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        }
        catch (Exception exception)
        {
            Serilog.Debugging.SelfLog.WriteLine("Configured log path unavailable: {0}", exception.GetType().Name);
            logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AlMoumenNursery", "Logs", "log-.txt");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            }
            catch (Exception fallbackException)
            {
                Serilog.Debugging.SelfLog.WriteLine("Fallback log path unavailable: {0}", fallbackException.GetType().Name);
                Log.Logger = new LoggerConfiguration().CreateLogger();
                return;
            }
        }

        var retainedFileCount = configuration.GetValue("Serilog:File:RetainedFileCountLimit", 31);
        var fileSizeLimitBytes = configuration.GetValue("Serilog:File:FileSizeLimitBytes", 10 * 1024 * 1024L);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty("AppVersion", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown")
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retainedFileCount,
                fileSizeLimitBytes: fileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj} {Properties}{NewLine}{Exception}")
            .CreateLogger();
    }

    private static void BackupDatabaseBeforeMigration(string databasePath)
    {
        var stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        try
        {
            File.Copy(databasePath, $"{databasePath}.bak-{stamp}", overwrite: false);
            foreach (var suffix in new[] { "-wal", "-shm" })
            {
                var sidecar = databasePath + suffix;
                if (File.Exists(sidecar))
                    File.Copy(sidecar, $"{sidecar}.bak-{stamp}", overwrite: false);
            }
            Log.Information("Database backup created before migration; databasePath={DatabasePath}; backupStamp={BackupStamp}", databasePath, stamp);
            foreach (var oldBackup in Directory.GetFiles(Path.GetDirectoryName(databasePath)!, Path.GetFileName(databasePath) + ".bak-*")
                         .OrderByDescending(File.GetLastWriteTimeUtc).Skip(5))
            {
                var oldStamp = oldBackup[(oldBackup.LastIndexOf(".bak-", StringComparison.Ordinal) + 5)..];
                File.Delete(oldBackup);
                foreach (var suffix in new[] { "-wal", "-shm" })
                {
                    var sidecarBackup = $"{databasePath}{suffix}.bak-{oldStamp}";
                    if (File.Exists(sidecarBackup)) File.Delete(sidecarBackup);
                }
            }
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Database backup failed before migration; databasePath={DatabasePath}", databasePath);
        }
    }

    private static void ShowStartupError()
    {
        MessageBox.Show(
            "تعذر تشغيل التطبيق. أعد المحاولة، وإذا استمرت المشكلة أرسل ملفات السجل للدعم الفني.",
            "تعذر تشغيل التطبيق", MessageBoxButtons.OK, MessageBoxIcon.Error,
            MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
    }

    private static string? FindProjectRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ElMomenNursery.sln")))
                return directory.FullName;
        }

        return null;
    }

    private static string WriteEmergencyStartupLog(Exception exception)
    {
        var localLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMoumenNursery", "Logs", $"startup-error-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(localLogPath)!);
            File.WriteAllText(localLogPath,
                $"وقت الخطأ: {DateTimeOffset.Now:O}{Environment.NewLine}مسار التطبيق: {AppContext.BaseDirectory}{Environment.NewLine}{exception}",
                System.Text.Encoding.UTF8);
            return localLogPath;
        }
        catch
        {
            var tempLogPath = Path.Combine(Path.GetTempPath(), $"AlMoumenNursery-startup-error-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            try
            {
                File.WriteAllText(tempLogPath, $"وقت الخطأ: {DateTimeOffset.Now:O}{Environment.NewLine}{exception}", System.Text.Encoding.UTF8);
                return tempLogPath;
            }
            catch { return localLogPath; }
        }
    }

    private static void ShowUnexpectedError(string logPath)
    {
        MessageBox.Show(
            $"حدث خطأ غير متوقع. أعد تشغيل التطبيق، وإذا استمرت المشكلة أرسل ملف السجل الموجود هنا:\n{logPath}",
            "تعذر تشغيل التطبيق",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error,
            MessageBoxDefaultButton.Button1,
            MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
    }
}
