using OrchestrationScenarios.Models;
using OrchestrationScenarios.Models.Messages;
using OrchestrationScenarios.Models.Messages.Content;
using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.Deltas.Content;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;
using StreamingUpdate = OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates.StreamingUpdate;

namespace OrchestrationScenarios.Utils;

public static class AgentRunner
{
    public static async Task RunAsync(Agent agent, List<ChatMessage> allMessages)
    {
        // Step 1: Find index of first agent message
        int agentIndex = allMessages.FindIndex(m => m.GetType() == typeof(AgentMessage));
        if (agentIndex == -1)
            throw new InvalidOperationException("No agent message found to stream.");

        // Step 2: Slice input messages up to first agent response
        var inputMessages = allMessages.Take(agentIndex).ToList();

        // Step 3: Display all pre-agent messages
        foreach (var message in inputMessages)
        {
            Console.ForegroundColor = GetColor(message.GetType());
            Console.Write($"<{GetTag(message.GetType())}>");
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
                        Console.Write($"<tool-call name=\"{call.Name}\">");
                        Console.ResetColor();
                        Console.Write(call.Arguments);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("</tool-call>");
                        Console.ResetColor();
                        break;

                    case ToolResultContent result:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"<tool-result\">");
                        Console.ResetColor();
                        Console.Write(result.Results);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("</tool-result>");
                        Console.ResetColor();
                        break;
                }
            }

            Console.WriteLine($"</{GetTag(message.GetType())}>");
            Console.ResetColor();
        }

        // Step 4: Stream the agent's actual response
        var stream = agent.StreamRunAsync(inputMessages);
        await DisplayStreamAsync(stream);
    }

    private static async Task DisplayStreamAsync(IAsyncEnumerable<StreamingUpdate> stream)
    {
        var openToolCalls = new Dictionary<string, (string name, bool opened)>();
        var callArgsBuffer = new Dictionary<string, List<string>>();

        await foreach (var part in stream)
        {
            switch (part)
            {
                case ChatMessageUpdate<AgentMessageDelta> chatMessageUpdate:
                    switch (chatMessageUpdate.Delta)
                    {
                        case StartStreamingOperation<AgentMessageDelta> startChatMessageStreamingOperation:
                            Console.ForegroundColor = GetColor(startChatMessageStreamingOperation.Value!.GetType());
                            Console.Write($"<{GetTag(startChatMessageStreamingOperation.Value!.GetType())}>");
                            Console.ResetColor();
                            break;
                        case SetStreamingOperation<AgentMessageDelta> setChatMessageStreamingOperation:
                            if (setChatMessageStreamingOperation.TypedValue!.Content == null || setChatMessageStreamingOperation.TypedValue.Content.Count == 0)
                            {
                                continue;
                            }

                            foreach (var content in setChatMessageStreamingOperation.TypedValue!.Content)
                            {
                                switch (content)
                                {
                                    case TextContent textContentDelta:
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.Write("<text>");
                                        Console.ResetColor();
                                        Console.Write(textContentDelta.Text);
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.Write("</text>");
                                        Console.ResetColor();
                                        break;

                                    case ToolCallContent toolCallContent:
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.Write($"<tool-call name=\"{toolCallContent.Name}\">");
                                        Console.ResetColor();
                                        Console.Write(toolCallContent.Arguments);
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.Write("</tool-call>");
                                        Console.ResetColor();
                                        break;
                                }
                            }
                            break;
                        case EndStreamingOperation<AgentMessageDelta> endChatMessageStreamingOperation:
                            Console.ForegroundColor = GetColor(endChatMessageStreamingOperation.Value!.GetType());
                            Console.WriteLine($"</{GetTag(endChatMessageStreamingOperation.Value!.GetType())}>");
                            Console.ResetColor();
                            break;
                    }
                    break;

                case ChatMessageUpdate<ToolMessageDelta> toolMessageUpdate:
                    switch (toolMessageUpdate.Delta)
                    {
                        case StartStreamingOperation<ToolMessageDelta> startToolMessageStreamingOperation:
                            Console.ForegroundColor = GetColor(startToolMessageStreamingOperation.Value!.GetType());
                            Console.Write($"<{GetTag(startToolMessageStreamingOperation.Value!.GetType())}>");
                            Console.ResetColor();
                            break;
                        case SetStreamingOperation<ToolMessageDelta> setToolMessageStreamingOperation:
                            if (setToolMessageStreamingOperation.TypedValue!.Content == null || setToolMessageStreamingOperation.TypedValue.Content.Count == 0)
                            {
                                continue;
                            }
                            
                            foreach (var content in setToolMessageStreamingOperation.TypedValue!.Content)
                            {
                                switch (content)
                                {
                                    case ToolResultContent toolResultContent:
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.Write("<tool-result>");
                                        Console.ResetColor();
                                        Console.Write(toolResultContent.Results);
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.Write("</tool-result>");
                                        Console.ResetColor();
                                        break;
                                }
                            }
                            break;

                        case EndStreamingOperation<ToolMessageDelta> endToolMessageStreamingOperation:
                            Console.ForegroundColor = GetColor(endToolMessageStreamingOperation.Value!.GetType());
                            Console.WriteLine($"</{GetTag(endToolMessageStreamingOperation.Value!.GetType())}>");
                            Console.ResetColor();
                            break;
                    }
                    break;

                case AIContentUpdate<TextContentDelta> aiContentUpdate:
                    switch (aiContentUpdate.Delta)
                    {
                        case StartStreamingOperation<TextContentDelta> startAiContentStreamingOperation:
                            Console.ForegroundColor = GetColor(startAiContentStreamingOperation.Value!.GetType());
                            Console.Write($"<{GetTag(startAiContentStreamingOperation.Value!.GetType())}>");
                            Console.ResetColor();
                            break;
                        case SetStreamingOperation<TextContentDelta> setAiContentStreamingOperation:
                            break;
                        case AppendStreamingOperation<TextContentDelta> appendAiContentStreamingOperation:
                            Console.Write(appendAiContentStreamingOperation.Value);
                            break;
                        case EndStreamingOperation<TextContentDelta> endAiContentStreamingOperation:
                            Console.ForegroundColor = GetColor(endAiContentStreamingOperation.Value!.GetType());
                            Console.Write($"</{GetTag(endAiContentStreamingOperation.Value!.GetType())}>");
                            Console.ResetColor();
                            break;
                        default:
                            // Handle other AI content types if needed
                            break;
                    }
                    break;
            }
        }
    }

    private static ConsoleColor GetColor(Type role) => role switch
    {
        var t when t == typeof(ToolMessageDelta) ||  t == typeof(ToolMessage) => ConsoleColor.Yellow,
        var t when t == typeof(AgentMessageDelta) || t == typeof(AgentMessage) => ConsoleColor.Magenta,
        var t when t == typeof(UserMessage) => ConsoleColor.Cyan,
        var t when t == typeof(TextContentDelta) || t == typeof(TextContent) => ConsoleColor.Yellow,
        _ => ConsoleColor.Gray
    };

    private static string GetTag(Type role) => role switch
    {
        var t when t == typeof(ToolMessageDelta) || t == typeof(ToolMessage) => "tool",
        var t when t == typeof(AgentMessageDelta) || t == typeof(AgentMessage) => "agent",
        var t when t == typeof(UserMessage) => "user",
        var t when t == typeof(TextContentDelta) || t == typeof(TextContent) => "text",
        _ => "unknown"
    };
}
