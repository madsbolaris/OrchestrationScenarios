using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ScenarioRunner;

public class ScenarioRunnerService : IHostedService
{
    private readonly IServiceProvider _provider;
    private readonly IHostApplicationLifetime _lifetime;

    public ScenarioRunnerService(IServiceProvider provider, IHostApplicationLifetime lifetime)
    {
        _provider = provider;
        _lifetime = lifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var scenarios = _provider.GetServices<IScenario>().ToList();

        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        if (args.Length > 0)
        {
            var scenario = scenarios.FirstOrDefault(s =>
                s.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));

            if (scenario != null)
                await scenario.RunOpenAIAsync();
            else
                Console.WriteLine($"Scenario '{args[0]}' not found.");

            _lifetime.StopApplication();
            return;
        }

        Console.WriteLine("Available Scenarios:");
        for (int i = 0; i < scenarios.Count; i++)
            Console.WriteLine($"{i + 1}. {scenarios[i].Name}");

        Console.Write("Select a scenario to run: ");
        if (int.TryParse(Console.ReadLine(), out int choice) &&
            choice >= 1 && choice <= scenarios.Count)
        {
            Console.Clear();

            await scenarios[choice - 1].RunOpenAIAsync();
        }
        else
        {
            Console.WriteLine("Invalid selection.");
        }

        _lifetime.StopApplication(); 
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
