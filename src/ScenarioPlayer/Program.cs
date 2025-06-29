using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScenarioPlayer.Startup;

namespace ScenarioPlayer;

class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, config) =>
            {
                config
                    .SetBasePath(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")))
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug(); // For VS/VS Code output
            })
            .ConfigureServices((context, services) =>
            {
                services.AddScenarioPlayerServices(context.Configuration);
            })
            .Build();

        await host.RunAsync();
    }
}
