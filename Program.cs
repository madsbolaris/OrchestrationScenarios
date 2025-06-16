using Microsoft.Extensions.DependencyInjection;
using OrchestrationScenarios.Agents;
using OrchestrationScenarios.Scenarios;
using System.Reflection;

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // Select which IAgent to use
        string agentType = args.Length > 1 ? args[1].ToLowerInvariant() : "basic";
        switch (agentType)
        {
            case "weather":
                services.AddSingleton<OrchestrationScenarios.Models.Agent, WeatherPersonAgent>();
                break;
            default:
                services.AddSingleton<OrchestrationScenarios.Models.Agent, BasicAgent>();
                break;
        }

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
