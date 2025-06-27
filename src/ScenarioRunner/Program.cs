using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using AgentsSdk.Runtime.Streaming;
using AgentsSdk.Runtime.Streaming.Providers.OpenAI;
using Microsoft.Extensions.Logging;
using AgentsSdk.Runtime;
using AgentsSdk.Models.Settings;
using Common;
using Microsoft.Extensions.Options;
using AgentsSdk.Runtime.Streaming.Providers.CopilotStudio;

namespace ScenarioRunner;

class Program
{
    private static readonly AgentRunnerKey[] AgentRunnerKeys =
    [
        AgentRunnerKey.OpenAI,
        AgentRunnerKey.CopilotStudio
    ];

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
                logging.AddDebug(); // VS/VS Code output
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureSettings(context, services);
                RegisterAgentClients(services);
                RegisterAgentRunners(services);
                RegisterHttpClient(services, context.Configuration);
                RegisterScenarios(services);
                services.AddHostedService<ScenarioRunnerService>();
            })
            .Build();

        await host.RunAsync();
    }

    private static void ConfigureSettings(HostBuilderContext context, IServiceCollection services)
    {
        services.Configure<OpenAISettings>(context.Configuration.GetSection("OpenAI"));
        services.Configure<DataverseSettings>(context.Configuration.GetSection("Dataverse"));
    }

    private static void RegisterAgentClients(IServiceCollection services)
    {
        services.AddSingleton<OpenAIStreamingClient>();
        services.AddSingleton<CopilotStudioStreamingClient>();
    }

    private static void RegisterAgentRunners(IServiceCollection services)
    {
        services.AddSingleton<AgentRunner<OpenAIStreamingClient>>();
        services.AddSingleton<AgentRunner<CopilotStudioStreamingClient>>();
    }

    private static void RegisterHttpClient(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("mcs")
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<DataverseSettings>>().Value;
                return new AddTokenHandler(settings);
            });
    }

    private static void RegisterScenarios(IServiceCollection services)
    {
        var scenarioDir = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Scenarios");
        if (!Directory.Exists(scenarioDir))
            return;

        var tools = new Dictionary<string, Delegate>
        {
            ["DateTime-Now"] = () => "2025-06-22 21:07:38"
        };

        foreach (var file in Directory.EnumerateFiles(scenarioDir, "*.liquid", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(file).Replace('_', ' ');

            services.AddTransient<IScenario>((sp) =>
            {
                return new Runner(sp, name, file, tools);
            });
        }
    }
}
