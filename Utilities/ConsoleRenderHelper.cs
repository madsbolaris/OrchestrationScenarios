namespace OrchestrationScenarios.Utilities;

using OrchestrationScenarios.Models.Messages;
using OrchestrationScenarios.Models.Messages.Content;
using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.Deltas.Content;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

public static class ConsoleRenderHelper
{
    public static async Task DisplayStreamAsync(IAsyncEnumerable<StreamingUpdate> stream)
    {
        await foreach (var part in stream)
        {
            switch (part)
            {
                case ChatMessageUpdate<AgentMessageDelta> chat when chat.Delta is { } delta:
                    HandleChatDelta(delta);
                    break;

                case ChatMessageUpdate<ToolMessageDelta> tool when tool.Delta is { } delta:
                    HandleChatDelta(delta);
                    break;

                case AIContentUpdate<TextContentDelta> ai when ai.Delta is { } delta:
                    HandleContentDelta(delta);
                    break;
            }
        }
    }

    public static async Task DisplayConversationAsync(List<ChatMessage> messages, IAsyncEnumerable<StreamingUpdate> stream)
    {
        foreach (var message in messages)
        {
            WriteTagOpen(message.GetType());
            foreach (var part in message.Content)
                WriteContent(part);
            WriteTagClose(message.GetType());
        }

        await DisplayStreamAsync(stream);
    }

    public static void WriteContent(AIContent content)
    {
        switch (content)
        {
            case TextContent text:
                WriteWrapped("text", text.Text);
                break;

            case ToolCallContent call:
                WriteWrapped($"tool-call name=\"{call.Name}\"", call.Arguments?.ToString());
                break;

            case ToolResultContent result:
                WriteWrapped("tool-result", result.Results?.ToString());
                break;
        }
    }

    private static void HandleChatDelta<T>(StreamingOperation<T> delta) where T : SystemGeneratedMessageDelta, new()
    {
        switch (delta)
        {
            case StartStreamingOperation<T> start:
                WriteTagOpen(start.Value?.GetType());
                break;

            case SetStreamingOperation<T> set when set.TypedValue?.Content is { Count: > 0 } contentList:
                foreach (var content in contentList)
                    WriteContent(content);
                break;

            case EndStreamingOperation<T> end:
                WriteTagClose(end.Value?.GetType());
                break;
        }
    }

    private static void HandleContentDelta(StreamingOperation<TextContentDelta> delta)
    {
        switch (delta)
        {
            case StartStreamingOperation<TextContentDelta> start:
                WriteTagOpen(start.Value?.GetType());
                break;

            case AppendStreamingOperation<TextContentDelta> append:
                Console.Write(append.Value);
                break;

            case EndStreamingOperation<TextContentDelta> end:
                WriteTagClose(end.Value?.GetType());
                break;
        }
    }

    private static void WriteWrapped(string tag, string? content)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"<{tag}>");
        Console.ResetColor();

        if (!string.IsNullOrEmpty(content))
            Console.Write(content);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"</{tag.Split(' ')[0]}>");
        Console.ResetColor();
    }

    private static void WriteTagOpen(Type? type)
    {
        if (type == null) return;
        Console.ForegroundColor = GetColor(type);
        Console.Write($"<{GetTag(type)}>");
        Console.ResetColor();
    }

    private static void WriteTagClose(Type? type)
    {
        if (type == null) return;
        Console.ForegroundColor = GetColor(type);
        Console.WriteLine($"</{GetTag(type)}>");
        Console.ResetColor();
    }

    private static ConsoleColor GetColor(Type role) => role switch
    {
        var t when t == typeof(ToolMessageDelta) || t == typeof(ToolMessage) => ConsoleColor.Yellow,
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
