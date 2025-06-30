using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AgentsSdk.Models.Settings;
using AgentsSdk.Runtime.Streaming;
using AgentsSdk.Runtime.Streaming.Providers.CopilotStudio;
using AgentsSdk.Runtime.Streaming.Providers.OpenAI;
using ScenarioPlayer.Core.Services;
using ScenarioPlayer.Parsing;
using ScenarioPlayer.UI;
using Common;

namespace ScenarioPlayer.Startup;

public static class ServiceRegistration
{
    public static IServiceCollection AddScenarioPlayerServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        // Bind settings
        services.Configure<OpenAISettings>(config.GetSection("OpenAI"));
        services.Configure<DataverseSettings>(config.GetSection("Dataverse"));

        // Add agent clients
        services.AddSingleton<IStreamingAgentClient, OpenAIStreamingClient>();
        services.AddSingleton<IStreamingAgentClient, CopilotStudioStreamingClient>();

        // Configure authenticated HTTP client
        services.AddHttpClient("mcs")
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<DataverseSettings>>().Value;
                return new AddTokenHandler(settings);
            });

        // Add scenario parsing and management services
        services.AddSingleton<YamlScenarioLoader>();
        services.AddSingleton<IScenarioManager, DefaultScenarioManager>();

        // UI logic
        services.AddSingleton<ScenarioExecutor>();
        services.AddSingleton<ScenarioUIController>();
        services.AddHostedService<ScenarioRunnerService>();

        return services;
    }
}
