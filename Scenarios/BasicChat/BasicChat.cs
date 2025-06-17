using Microsoft.Extensions.AI;
using OrchestrationScenarios.Agents;
using OrchestrationScenarios.Models;
using OrchestrationScenarios.Models.ContentParts;

namespace OrchestrationScenarios.Scenarios;

public class BasicChatScenario : IScenario
{
    private readonly Agent _agent;
    public string Name => "Basic Chat Scenario";

    public BasicChatScenario(BasicAgent agent)
    {
        _agent = agent;
    }

    public async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("<user>");

        Console.ResetColor();
        var input = "Hello";
        Console.Write(input);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("</user>");

        string? currentMessageId = null;
        AuthorRole? currentRole = null;

        await foreach (var part in _agent.StreamRunAsync([
            new Message
            {
                Role = AuthorRole.User,
                Content = [new Models.ContentParts.TextContent("what's the weather in Seattle today?")]
            }
        ]))
        {
            switch (part)
            {
                case StreamedStartContent start:
                    currentMessageId = start.MessageId;
                    currentRole = start.AuthorRole;

                    Console.ForegroundColor = GetColor(start.AuthorRole);
                    Console.Write($"<{GetTag(start.AuthorRole)}>");
                    Console.ResetColor();
                    break;

                case StreamedTextContent text:
                    Console.Write(text.Text);
                    break;

                case StreamedEndContent end:
                    if (currentMessageId == end.MessageId && currentRole is not null)
                    {
                        Console.ForegroundColor = GetColor(currentRole.Value);
                        Console.WriteLine($"</{GetTag(currentRole.Value)}>");
                        Console.ResetColor();
                        currentMessageId = null;
                        currentRole = null;
                    }
                    break;
            }
        }
    }



    private static ConsoleColor GetColor(AuthorRole role) => role switch
    {
        AuthorRole.Tool => ConsoleColor.Yellow,
        AuthorRole.Agent => ConsoleColor.Magenta,
        _ => ConsoleColor.Gray
    };

    private static string GetTag(AuthorRole role) => role switch
    {
        AuthorRole.Tool => "tool",
        AuthorRole.Agent => "agent",
        _ => "unknown"
    };
}
