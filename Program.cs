using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

using OrchestrationScenarios.Agents;
using OrchestrationScenarios.Models;
using OrchestrationScenarios.Runtime;
using OrchestrationScenarios.Scenarios;
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
        services.AddSingleton<OpenAIResponseClient>(provider =>
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

        // Agents
        services.AddSingleton<BasicAgent>();
        services.AddSingleton<WeatherPersonAgent>();

        // Scenarios
        var scenarioTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IScenario).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in scenarioTypes)
            services.AddTransient(typeof(IScenario), type);

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
