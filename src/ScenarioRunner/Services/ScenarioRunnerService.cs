using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ScenarioRunner.Controllers;
using Terminal.Gui;
using ScenarioRunner.Interfaces;

namespace ScenarioRunner.Services;

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
        var scenarios = _provider.GetServices<IScenario>().OrderBy(s => s.Name).ToList();

        Application.Init();

        var controller = new ScenarioController(_provider, _lifetime);
        await controller.StartAsync(scenarios);

        Application.Shutdown();
    }


    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
