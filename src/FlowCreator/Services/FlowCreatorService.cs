using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Runtime;
using FlowCreator.Models;
using FlowCreator.Services;
using Microsoft.Extensions.Hosting;

public class FlowCreatorService(AIDocumentService documentService, AgentRunner runner, CopilotFactory copilotFactory, IHostApplicationLifetime lifetime) : IHostedService
{

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage>();
        var document = documentService.AddAIDocument(new AIDocument());
        var copilot = copilotFactory.CreateCopilot(document.Id);

        while (true)
        {
            await runner.RunAsync(copilot, messages);
            Console.Write("<user>");
            var input = Console.ReadLine();
            Console.WriteLine("</user>");

            if (string.IsNullOrWhiteSpace(input))
            {
                break;
            }
            
            messages.Add(new UserMessage
            {
                Content = [new TextContent { Text = input }]
            });
        }

        lifetime.StopApplication();
    }
    
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}