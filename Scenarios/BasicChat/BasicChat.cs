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

        // Track open tool-call tags and their args
        var openToolCalls = new Dictionary<string, (string name, bool opened)>();
        var callArgsBuffer = new Dictionary<string, List<string>>();

        await foreach (var part in _agent.StreamRunAsync([
            new Message
            {
                Role = AuthorRole.User,
                Content = [new TextContent("what's today's date? Then search for today's weather in Seattle, WA.")]
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

                case StreamedFunctionCallContent funcCall:
                    var callId = funcCall.MessageId;

                    if (!callArgsBuffer.TryGetValue(callId, out var buffer))
                    {
                        buffer = [];
                        callArgsBuffer[callId] = buffer;
                    }

                    buffer.Add(funcCall.ArgumentDelta);

                    if (funcCall.PluginName is not null && funcCall.FunctionName is not null)
                    {
                        var name = $"{funcCall.PluginName}-{funcCall.FunctionName}";
                        openToolCalls[callId] = (name, false);
                    }

                    // Print opening tag if we haven't yet
                    if (openToolCalls.TryGetValue(callId, out var entry) && !entry.opened)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"<tool-call name=\"{entry.name}\">");
                        Console.ResetColor();
                        openToolCalls[callId] = (entry.name, true);
                    }

                    // Print the current delta
                    Console.Write(funcCall.ArgumentDelta);
                    break;

                case StreamedFunctionResultContent funcResult:
                    // Print tool output
                    Console.Write(funcResult.Result);
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
