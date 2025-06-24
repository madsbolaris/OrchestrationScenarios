using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

using OrchestrationScenarios.Models;
using OrchestrationScenarios.Runtime;
using OrchestrationScenarios.Runtime.Streaming;
using OrchestrationScenarios.Runtime.Streaming.Providers.OpenAI;

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // Bind OpenAI configuration
        var openAIConfig = new OpenAIConfiguration();
        configuration.Bind("OpenAI", openAIConfig.OpenAI);
        openAIConfig.ModelId = configuration["OpenAI:ModelId"] ?? "gpt-4";
        services.AddSingleton(openAIConfig);

        // Register OpenAI client
        services.AddSingleton(provider =>
        {
            var config = provider.GetRequiredService<OpenAIConfiguration>();
            return new OpenAIResponseClient(
                model: config.ModelId,
                credential: new ApiKeyCredential(config.OpenAI.ApiKey),
                options: new OpenAIClientOptions()
            );
        });

        // Core services
        services.AddSingleton<IStreamingAgentClient, OpenAIStreamingClient>();
        services.AddSingleton<AgentRunner>();

        // Scenarios
        // Register file-based scenarios from a folder
        var tools = new Dictionary<string, Delegate>
        {
            { "DateTime-Now", () => "2025-06-22 21:07:38" }
            // Add more mock built-in tools here
        };

        var scenarioFolder = Path.Combine(Directory.GetCurrentDirectory(), "Scenarios");
        foreach (var file in Directory.EnumerateFiles(scenarioFolder, "*.liquid", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(file).Replace('_', ' ');
            services.AddTransient<IScenario>(provider =>
            {
                var runner = provider.GetRequiredService<AgentRunner>();
                return new ScenarioRunner(name, file, runner, tools);
            });
        }


        var provider = services.BuildServiceProvider();
        var scenarios = provider.GetServices<IScenario>().ToList();

        // Try running a scenario by name
        if (args.Length > 0)
        {
            var scenario = scenarios.FirstOrDefault(s =>
                s.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));

            if (scenario != null)
            {
                await scenario.RunAsync();
                return;
            }

            Console.WriteLine($"Scenario '{args[0]}' not found.");
            return;
        }

        // Interactive fallback
        Console.WriteLine("Available Scenarios:");
        for (int i = 0; i < scenarios.Count; i++)
            Console.WriteLine($"{i + 1}. {scenarios[i].Name}");

        Console.Write("Select a scenario to run: ");
        if (int.TryParse(Console.ReadLine(), out int choice) &&
            choice >= 1 && choice <= scenarios.Count)
        {
            Console.Clear();
            await scenarios[choice - 1].RunAsync();
        }
        else
        {
            Console.WriteLine("Invalid selection.");
        }
    }
}