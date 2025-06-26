using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Runs.Responses.Deltas.Content;
using AgentsSdk.Models.Runs.Responses.StreamingOperations;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;

namespace AgentsSdk.Helpers;

public static class ConsoleRenderHelper
{
    private static readonly Dictionary<string, string> _toolCallIdMap = new();
    private static int _toolCallIdCounter = 1;

    private static string GetShortToolCallId(string longId)
    {
        if (!_toolCallIdMap.TryGetValue(longId, out var shortId))
        {
            shortId = _toolCallIdCounter.ToString("D4");
            _toolCallIdMap[longId] = shortId;
            _toolCallIdCounter++;
        }
        return shortId;
    }


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
                    HandleTextContentDelta(delta);
                    break;

                case AIContentUpdate<ToolCallContentDelta> ai when ai.Delta is { } delta:
                    HandleToolCallContentDelta(delta);
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
                var shortId = GetShortToolCallId(call.ToolCallId);
                WriteWrapped($"tool-call name=\"{call.Name}\" id=\"{shortId}\"", call.Arguments?.ToString());
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
                if (start.Value is ToolMessageDelta toolMessageDelta)
                {
                    var shortId = GetShortToolCallId(toolMessageDelta.ToolCallId);
                    WriteTagOpen(toolMessageDelta.GetType(), $"for=\"{shortId}\"");
                }
                else
                {
                    WriteTagOpen(start.Value?.GetType());
                }
                break;

            case SetStreamingOperation<T> set when set.TypedValue?.Content is { Count: > 0 } contentList:
                foreach (var content in contentList)
                    WriteContent(content);
                break;

            case EndStreamingOperation<T> end:
                WriteTagClose(end.Value?.GetType());
                Console.WriteLine(); // Ensure a new line after closing tag
                break;
        }
    }

    private static void HandleTextContentDelta(StreamingOperation<TextContentDelta> delta)
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

    private static void HandleToolCallContentDelta(StreamingOperation<ToolCallContentDelta> delta)
    {
        switch (delta)
        {
            case StartStreamingOperation<ToolCallContentDelta> start:
                if (start.Value is ToolCallContentDelta toolCall)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    var shortId = GetShortToolCallId(toolCall.ToolCallId);
                    var tag = $"<tool-call name=\"{toolCall.Name}\" id=\"{shortId}\">";
                    Console.Write(tag);
                    Console.ResetColor();
                }
                break;

            case AppendStreamingOperation<ToolCallContentDelta> append:
                Console.Write(append.Value);
                break;

            case EndStreamingOperation<ToolCallContentDelta>:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("</tool-call>");
                Console.ResetColor();
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

    public static void WriteTagOpen(Type? type, string? attributes = null)
    {
        if (type == null) return;
        Console.ForegroundColor = GetColor(type);
        Console.Write($"<{GetTag(type)}");
        if (!string.IsNullOrEmpty(attributes))
        {
            Console.Write($" {attributes}");
        }
        Console.Write(">");
        Console.ResetColor();
    }

    public static void WriteTagClose(Type? type)
    {
        if (type == null) return;
        Console.ForegroundColor = GetColor(type);
        Console.Write($"</{GetTag(type)}>");
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
