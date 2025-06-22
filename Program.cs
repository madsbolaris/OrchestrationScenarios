using Microsoft.Extensions.DependencyInjection;
using OrchestrationScenarios.Agents;
using Microsoft.Extensions.Configuration;
using OrchestrationScenarios.Scenarios;
using System.Reflection;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;
using OrchestrationScenarios.Models;
using OrchestrationScenarios.Runtime;

class Program
{
    static async Task Main(string[] args)
    {

        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // 2. Bind configuration to a typed object
        var openAIConfig = new OpenAIConfiguration();
        configuration.Bind("OpenAI", openAIConfig.OpenAI);
        openAIConfig.ModelId = configuration["OpenAI:ModelId"] ?? "gpt-4";

        // 3. Register it and the OpenAI client in DI
        services.AddSingleton(openAIConfig);

        services.AddSingleton<OpenAIResponseClient>((provider) =>
        {
            var config = provider.GetRequiredService<OpenAIConfiguration>();
            var options = new OpenAIClientOptions();
            return new (
                model: config.ModelId,
                credential: new ApiKeyCredential(config.OpenAI.ApiKey),
                options: options
            );
        });

        services.AddSingleton<AgentRunner>();

        services.AddSingleton<BasicAgent>();
        services.AddSingleton<WeatherPersonAgent>();

        // Register all IScenario implementations
        var scenarioTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IScenario).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in scenarioTypes)
            services.AddTransient(typeof(IScenario), type);

        var provider = services.BuildServiceProvider();
        var scenarios = provider.GetServices<IScenario>().ToList();

        // Scenario selection by name (arg[0])
        if (args.Length > 0)
        {
            var scenarioName = args[0];
            var selected = scenarios.FirstOrDefault(s =>
                s.Name.Equals(scenarioName, StringComparison.OrdinalIgnoreCase));

            if (selected != null)
            {
                await selected.RunAsync();
                return;
            }

            Console.WriteLine($"Scenario '{scenarioName}' not found.");
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
