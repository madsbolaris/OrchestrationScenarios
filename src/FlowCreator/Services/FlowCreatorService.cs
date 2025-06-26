using AgentsSdk.Helpers;
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
            ConsoleRenderHelper.WriteTagOpen(typeof(UserMessage));

            string input = string.Empty;
            int startLeft = Console.CursorLeft;
            ConsoleKeyInfo key;

            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (input.Length > 0 && Console.CursorLeft > startLeft)
                    {
                        input = input[..^1];
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.Write(' ');
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    }
                    continue;
                }

                if (!char.IsControl(key.KeyChar))
                {
                    Console.Write(key.KeyChar);
                    input += key.KeyChar;
                }
            }

            ConsoleRenderHelper.WriteTagClose(typeof(UserMessage));
            
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