using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Runs.Responses.Deltas.Content;
using AgentsSdk.Models.Runs.Responses.StreamingOperations;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using ScenarioRunner.Views;

namespace ScenarioRunner.Helpers;

public static class TerminalRenderHelper
{
    private static readonly Dictionary<string, string> _toolCallIdMap = [];
    private static int _toolCallIdCounter = 1;

    private static string GetShortToolCallId(string longId) =>
        _toolCallIdMap.TryGetValue(longId, out var shortId)
            ? shortId
            : (_toolCallIdMap[longId] = _toolCallIdCounter++.ToString("D4"));

    public static async Task DisplayStreamAsync(IAsyncEnumerable<StreamingUpdate> stream, StreamingOutputView output)
    {
        await foreach (var update in stream)
        {
            switch (update)
            {
                case ChatMessageUpdate<AgentMessageDelta> chat:
                    HandleChatDelta(chat.Delta, output);
                    break;
                case ChatMessageUpdate<ToolMessageDelta> tool:
                    HandleChatDelta(tool.Delta, output);
                    break;
                case AIContentUpdate<TextContentDelta> text:
                    HandleAIContentDelta(text.Delta, output);
                    break;
                case AIContentUpdate<ToolCallContentDelta> toolCall:
                    HandleAIContentDelta(toolCall.Delta, output);
                    break;
            }
            await Task.Yield();
        }
    }

    private static void WriteWrapped(string tag, string? content, StreamingOutputView output)
    {
        output.AppendRaw($"<{tag}>");
        if (!string.IsNullOrEmpty(content))
            output.AppendRaw(content);
        output.AppendRaw($"</{tag.Split(' ')[0]}>");
    }

    private static void WriteTagOpen(Type? type, StreamingOutputView output, string? attributes = null)
    {
        if (type == null) return;
        var tag = GetTag(type);
        var line = string.IsNullOrEmpty(attributes) ? $"<{tag}>" : $"<{tag} {attributes}>";
        output.AppendRaw(line);
    }

    private static void WriteTagClose(Type? type, StreamingOutputView output)
    {
        if (type == null) return;
        output.AppendRaw($"</{GetTag(type)}>");
    }

    private static void HandleChatDelta<T>(StreamingOperation<T> delta, StreamingOutputView output) where T : SystemGeneratedMessageDelta
    {
        switch (delta)
        {
            case StartStreamingOperation<T> start:
                var toolMessage = start.Value as ToolMessageDelta;
                var attrs = toolMessage != null ? $"for=\"{GetShortToolCallId(toolMessage.ToolCallId)}\"" : null;
                WriteTagOpen(start.Value?.GetType(), output, attrs);
                break;

            case SetStreamingOperation<T> set:
                foreach (var content in ((SystemGeneratedMessageDelta)set.Value).Content!)
                    WriteContent(content, output);
                break;

            case EndStreamingOperation<T> end:
                WriteTagClose(end.Value?.GetType(), output);
                output.AppendRaw("\n");
                break;
        }
    }

    private static void HandleAIContentDelta<T>(StreamingOperation<T> delta, StreamingOutputView output) where T : AIContentDelta
    {
        switch (delta)
        {
            case StartStreamingOperation<T> start:
                var toolCall = start.Value as ToolCallContentDelta;
                var attrs = toolCall != null ? $"name=\"{toolCall.Name}\" id=\"{GetShortToolCallId(toolCall.ToolCallId)}\"" : null;
                WriteTagOpen(start.Value?.GetType(), output, attrs);
                break;

            case AppendStreamingOperation<T> append:
                output.AppendRaw(append.Value!.ToString()!);
                break;

            case EndStreamingOperation<T> end:
                WriteTagClose(end.Value?.GetType(), output);
                break;
        }
    }

    private static void WriteContent(AIContent content, StreamingOutputView output)
    {
        switch (content)
        {
            case TextContent text:
                WriteWrapped("text", text.Text, output);
                break;
            case ToolCallContent call:
                WriteWrapped($"tool-call name=\"{call.Name}\" id=\"{GetShortToolCallId(call.ToolCallId)}\"", call.Arguments?.ToString(), output);
                break;
            case ToolResultContent result:
                WriteWrapped("tool-result", result.Results?.ToString(), output);
                break;
        }
    }

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
}
