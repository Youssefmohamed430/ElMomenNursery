using ElMomenNursery.Infrastructure;
using ElMomenNursery.Infrastructure.Persistence;
using ElMomenNursery.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();
        var services = new ServiceCollection();
        services.AddNurseryInfrastructure(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        using (var scope = serviceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<NurseryDbContext>().Database.Migrate();
        }

        using var login = new LoginForm();
        if (login.ShowDialog() == DialogResult.OK)
        {
            Application.Run(new MainForm());
        }
    }
}
