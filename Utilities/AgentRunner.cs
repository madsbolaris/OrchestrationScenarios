using OrchestrationScenarios.Models;
using OrchestrationScenarios.Models.ContentParts;

namespace OrchestrationScenarios.Utils;

public static class AgentRunner
{
    public static async Task RunAsync(Agent agent, List<Message> allMessages)
    {
        // Step 1: Find index of first agent message
        int agentIndex = allMessages.FindIndex(m => m.Role == AuthorRole.Agent);
        if (agentIndex == -1)
            throw new InvalidOperationException("No agent message found to stream.");

        // Step 2: Slice input messages up to first agent response
        var inputMessages = allMessages.Take(agentIndex).ToList();

        // Step 3: Display all pre-agent messages
        foreach (var message in inputMessages)
        {
            Console.ForegroundColor = GetColor(message.Role);
            Console.Write($"<{GetTag(message.Role)}>");
            Console.ResetColor();

            foreach (var part in message.Content)
            {
                switch (part)
                {
                    case TextContent text:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("<text>");
                        Console.ResetColor();
                        Console.Write(text.Text);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("</text>");
                        Console.ResetColor();
                        break;

                    case ToolCallContent call:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"<tool-call name=\"{call.FunctionName}\">");
                        Console.ResetColor();
                        Console.Write(call.Arguments);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("</tool-call>");
                        Console.ResetColor();
                        break;

                    case ToolResultContent result:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"<tool-result id=\"{result.CallId}\">");
                        Console.ResetColor();
                        Console.Write(result.FunctionResult);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("</tool-result>");
                        Console.ResetColor();
                        break;
                }
            }

            Console.WriteLine($"</{GetTag(message.Role)}>");
            Console.ResetColor();
        }

        // Step 4: Stream the agent's actual response
        var stream = agent.StreamRunAsync(inputMessages);
        await DisplayStreamAsync(stream);
    }

    private static async Task DisplayStreamAsync(IAsyncEnumerable<StreamedContentPart> stream)
    {
        string? currentMessageId = null;
        AuthorRole? currentRole = null;
        var openToolCalls = new Dictionary<string, (string name, bool opened)>();
        var callArgsBuffer = new Dictionary<string, List<string>>();

        await foreach (var part in stream)
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

                    if (openToolCalls.TryGetValue(callId, out var entry) && !entry.opened)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"<tool-call name=\"{entry.name}\">");
                        Console.ResetColor();
                        openToolCalls[callId] = (entry.name, true);
                    }

                    Console.Write(funcCall.ArgumentDelta);
                    break;

                case StreamedFunctionResultContent funcResult:
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
        AuthorRole.User => ConsoleColor.Cyan,
        _ => ConsoleColor.Gray
    };

    private static string GetTag(AuthorRole role) => role switch
    {
        AuthorRole.Tool => "tool",
        AuthorRole.Agent => "agent",
        AuthorRole.User => "user",
        _ => "unknown"
    };
}
