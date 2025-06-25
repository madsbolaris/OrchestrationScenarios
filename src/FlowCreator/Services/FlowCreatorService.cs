using AgentsSdk.Runtime;
using FlowCreator.Models;
using FlowCreator.Services;
using Microsoft.Extensions.Hosting;

public class FlowCreatorService(AIDocumentService documentService, AgentRunner runner, CopilotFactory copilotFactory, IHostApplicationLifetime lifetime) : IHostedService
{

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var document = documentService.AddAIDocument(new AIDocument());
        var copilot = copilotFactory.CreateCopilot(document.Id);

        await runner.RunAsync(copilot);

        lifetime.StopApplication();
    }
    
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}