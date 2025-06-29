using Microsoft.Extensions.Hosting;

namespace ScenarioPlayer.UI;

public class ScenarioRunnerService(
    ScenarioUIController controller,
    IHostApplicationLifetime lifetime) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await controller.RunAsync(cancellationToken);
        lifetime.StopApplication(); // Ensure exit after TUI ends
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
