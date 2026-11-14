using ElMomenNursery.Infrastructure;
using ElMomenNursery.Infrastructure.Persistence;
using ElMomenNursery.Forms;
using ElMomenNursery.Infrastructure.Security;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ElMomenNursery;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();
            ConfigureLogging(configuration);
            Log.Information("Application starting");

            ApplicationConfiguration.Initialize();
            System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            System.Windows.Forms.Application.ThreadException += (_, eventArgs) =>
            {
                Log.Error(eventArgs.Exception, "Unhandled Windows Forms exception");
                ShowUnexpectedError();
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
            services.AddScoped<IChildService, ChildService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddNurseryInfrastructure(configuration);
            using var serviceProvider = services.BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<NurseryDbContext>();
                db.Database.Migrate();
                AuthSeed.InitializeAsync(db).GetAwaiter().GetResult();
            }

            var session = serviceProvider.GetRequiredService<ISessionManager>();
            while (true)
            {
                using (var loginScope = serviceProvider.CreateScope())
                using (var login = new LoginForm(loginScope.ServiceProvider.GetRequiredService<IAuthService>()))
                    if (login.ShowDialog() != DialogResult.OK) break;

                using var appScope = serviceProvider.CreateScope();
                var auth = appScope.ServiceProvider.GetRequiredService<IAuthService>();
                System.Windows.Forms.Application.Run(new MainForm(session.CurrentUser!, () => auth.LogoutAsync(),
                    appScope.ServiceProvider.GetRequiredService<IAccountManagementService>(),
                    appScope.ServiceProvider.GetRequiredService<IChildService>(),
                    appScope.ServiceProvider.GetRequiredService<IEmployeeService>()));
                if (session.IsAuthenticated) auth.LogoutAsync().GetAwaiter().GetResult();
            }

            Log.Information("Application stopped normally");
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Application terminated during startup or execution");
            ShowUnexpectedError();
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureLogging(IConfiguration configuration)
    {
        var configuredPath = configuration["Serilog:File:Path"] ?? "Logs/log-.txt";
        var logPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
        logPath = Path.GetFullPath(logPath);
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        var retainedFileCount = configuration.GetValue("Serilog:File:RetainedFileCountLimit", 31);
        var fileSizeLimitBytes = configuration.GetValue("Serilog:File:FileSizeLimitBytes", 10 * 1024 * 1024L);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retainedFileCount,
                fileSizeLimitBytes: fileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private static void ShowUnexpectedError()
    {
        MessageBox.Show(
            "حدث خطأ غير متوقع. أعد تشغيل التطبيق، وإذا استمرت المشكلة أرسل ملفات السجل للدعم الفني.",
            "تعذر تشغيل التطبيق",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error,
            MessageBoxDefaultButton.Button1,
            MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
    }
}
