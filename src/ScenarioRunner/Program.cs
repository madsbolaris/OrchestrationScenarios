using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;
using AgentsSdk.Models;
using AgentsSdk.Runtime.Streaming;
using AgentsSdk.Runtime.Streaming.Providers.OpenAI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using AgentsSdk.Runtime;

namespace ScenarioRunner;

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
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.ClearProviders();

                // Add debug logger (shows up in VS/VS Code debug pane)
                logging.AddDebug();
            })
            .ConfigureServices((context, services) =>
            {
                // Bind and register OpenAI configuration
                services.Configure<OpenAISettings>(context.Configuration.GetSection("OpenAI"));

                services.AddSingleton<IStreamingAgentClient, OpenAIStreamingClient>();
                services.AddSingleton<AgentRunner>();

                // Register file-based scenarios
                var tools = new Dictionary<string, Delegate>
                {
                    { "DateTime-Now", () => "2025-06-22 21:07:38" }
                };

                var scenarioFolder = Path.Combine(Directory.GetCurrentDirectory(), "Resources/Scenarios");
                foreach (var file in Directory.EnumerateFiles(scenarioFolder, "*.liquid", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileNameWithoutExtension(file).Replace('_', ' ');
                    services.AddTransient<IScenario>(provider =>
                    {
                        var runner = provider.GetRequiredService<AgentRunner>();
                        return new Runner(name, file, runner, tools);
                    });
                }

                services.AddHostedService<ScenarioRunnerService>();
            })
            .Build();

        await host.RunAsync();
    }
}
