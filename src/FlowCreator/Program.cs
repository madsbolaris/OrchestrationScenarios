using System.Text.Json;
using FlowCreator.Models;
using FlowCreator.Services;
using FlowCreator.Workflows.Spec;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using AgentsSdk.Runtime;
using AgentsSdk.Models;
using AgentsSdk.Runtime.Streaming;
using AgentsSdk.Runtime.Streaming.Providers.OpenAI;

// Entry point
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
                // Add services
                services.AddTransient((sp)=>
                {
                    return new Kernel(sp);
                });
                
                // Bind and register OpenAI configuration
                services.Configure<OpenAISettings>(context.Configuration.GetSection("OpenAI"));
                services.Configure<AaptConnectorsSettings>(context.Configuration.GetSection("AaptConnectors"));
                services.Configure<DataverseSettings>(context.Configuration.GetSection("Dataverse"));

                services.AddSingleton<IStreamingAgentClient, OpenAIStreamingClient>();
                services.AddSingleton<AIDocumentService>();
                services.AddSingleton<CopilotFactory>();
                services.AddSingleton<AgentRunner>();
                services.AddSingleton(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
                services.AddHostedService<FlowCreatorService>();
            })
            .Build();

        await host.RunAsync();
    }
}
