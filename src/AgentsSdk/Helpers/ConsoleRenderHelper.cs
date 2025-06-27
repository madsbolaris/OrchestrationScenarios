namespace AgentsSdk.Helpers;

using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Runs.Responses.Deltas.Content;
using AgentsSdk.Models.Runs.Responses.StreamingOperations;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;

public static class ConsoleRenderHelper
{
    private static readonly Dictionary<string, string> _toolCallIdMap = [];
    private static int _toolCallIdCounter = 1;

    private static string GetShortToolCallId(string longId) =>
        _toolCallIdMap.TryGetValue(longId, out var shortId)
            ? shortId
            : (_toolCallIdMap[longId] = _toolCallIdCounter++.ToString("D4"));

    public static async Task DisplayStreamAsync(IAsyncEnumerable<StreamingUpdate> stream)
    {
        await foreach (var update in stream)
        {
            switch (update)
            {
                case ChatMessageUpdate<AgentMessageDelta> chat:
                    HandleChatDelta(chat.Delta);
                    break;
                case ChatMessageUpdate<ToolMessageDelta> tool:
                    HandleChatDelta(tool.Delta);
                    break;
                case AIContentUpdate<TextContentDelta> text:
                    HandleAIContentDelta(text.Delta);
                    break;
                case AIContentUpdate<ToolCallContentDelta> toolCall:
                    HandleAIContentDelta(toolCall.Delta);
                    break;
            }
        }
    }

    public static void WriteContent(AIContent content)
    {
        switch (content)
        {
            case TextContent text:
                WriteWrapped("text", text.Text);
                break;

            case ToolCallContent call:
                WriteWrapped($"tool-call name=\"{call.Name}\" id=\"{GetShortToolCallId(call.ToolCallId)}\"", call.Arguments?.ToString());
                break;

            case ToolResultContent result:
                WriteWrapped("tool-result", result.Results?.ToString());
                break;
        }
    }

    private static void HandleChatDelta<T>(StreamingOperation<T> delta) where T : SystemGeneratedMessageDelta
    {
        switch (delta)
        {
            case StartStreamingOperation<T> start:
                var toolMessage = start.Value as ToolMessageDelta;
                var attributes = toolMessage != null ? $"for=\"{GetShortToolCallId(toolMessage.ToolCallId)}\"" : null;
                WriteTagOpen(start.Value?.GetType(), attributes);
                break;

            case SetStreamingOperation<T> set:
                foreach (var content in ((SystemGeneratedMessageDelta)set.Value).Content!)
                    WriteContent(content);
                break;

            case EndStreamingOperation<T> end:
                WriteTagClose(end.Value?.GetType());
                Console.WriteLine();
                break;
        }
    }

    private static void HandleAIContentDelta<T>(StreamingOperation<T> delta) where T : AIContentDelta
    {
        switch (delta)
        {
            case StartStreamingOperation<T> start:
            {
                using var _ = SetConsoleColor(ConsoleColor.Yellow);
                var attributes = start.Value is ToolCallContentDelta toolCall
                    ? $"name=\"{toolCall.Name}\" id=\"{GetShortToolCallId(toolCall.ToolCallId)}\""
                    : null;
                WriteTagOpen(start.Value?.GetType(), attributes);
                break;
            }

            case AppendStreamingOperation<T> append:
            {
                Console.Write(append.Value);
                break;
            }

            case EndStreamingOperation<T> end:
            {
                using var _endColor = SetConsoleColor(ConsoleColor.Yellow);
                WriteTagClose(end.Value?.GetType());
                break;
            }
        }
    }


    private static void WriteWrapped(string tag, string? content)
    {
        using var _ = SetConsoleColor(ConsoleColor.Yellow);
        Console.Write($"<{tag}>");
        Console.ResetColor();

        if (!string.IsNullOrEmpty(content))
            Console.Write(content);

        using var __ = SetConsoleColor(ConsoleColor.Yellow);
        Console.Write($"</{tag.Split(' ')[0]}>");
        Console.ResetColor();
    }

    public static void WriteTagOpen(Type? type, string? attributes = null)
    {
        if (type == null) return;
        using var _ = SetConsoleColor(GetColor(type));
        Console.Write($"<{GetTag(type)}");
        if (!string.IsNullOrEmpty(attributes))
            Console.Write($" {attributes}");
        Console.Write(">");
    }

    public static void WriteTagClose(Type? type)
    {
        if (type == null) return;
        using var _ = SetConsoleColor(GetColor(type));
        Console.Write($"</{GetTag(type)}>");
    }

    private static ConsoleColor GetColor(Type type) => type switch
    {
        var t when t == typeof(ToolMessageDelta) || t == typeof(ToolMessage) => ConsoleColor.Yellow,
        var t when t == typeof(AgentMessageDelta) || t == typeof(AgentMessage) => ConsoleColor.Magenta,
        var t when t == typeof(UserMessage) => ConsoleColor.Cyan,
        var t when t == typeof(TextContentDelta) || t == typeof(TextContent) => ConsoleColor.Yellow,
        var t when t == typeof(ToolCallContentDelta) || t == typeof(ToolCallContent) => ConsoleColor.Yellow,
        var t when t == typeof(ToolResultContent) => ConsoleColor.Green,
        _ => ConsoleColor.Gray
    };

    private static string GetTag(Type type) => type switch
    {
        var t when t == typeof(ToolMessageDelta) || t == typeof(ToolMessage) => "tool",
        var t when t == typeof(AgentMessageDelta) || t == typeof(AgentMessage) => "agent",
        var t when t == typeof(UserMessage) => "user",
        var t when t == typeof(TextContentDelta) || t == typeof(TextContent) => "text",
        var t when t == typeof(ToolCallContentDelta) || t == typeof(ToolCallContent) => "tool-call",
        var t when t == typeof(ToolResultContent) => "tool-result",
        _ => "unknown"
    };

    private static DisposableAction SetConsoleColor(ConsoleColor color)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = color;
        return new DisposableAction(() => Console.ForegroundColor = original);
    }

    private class DisposableAction : IDisposable
    {
        private readonly Action _onDispose;
        public DisposableAction(Action onDispose) => _onDispose = onDispose;
        public void Dispose() => _onDispose();
    }
}
