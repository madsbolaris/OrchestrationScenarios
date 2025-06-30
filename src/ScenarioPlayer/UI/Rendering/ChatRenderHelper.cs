using System.Text.Encodings.Web;
using System.Text.Json;
using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Runs.Responses.Deltas.Content;
using AgentsSdk.Models.Runs.Responses.StreamingOperations;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using Common.Views;

namespace ScenarioPlayer.UI.Rendering;

public static class ChatRenderHelper
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

    public static void RenderStaticMessage(ChatMessage message, ChatView chat)
    {
        switch (message)
        {
            case UserMessage:
                chat.StartMessage("User");
                break;
            case AgentMessage:
                chat.StartMessage("Agent");
                break;
            case ToolMessage tool:
                chat.StartMessage("Tool", tool.ToolType, GetShortToolCallId(tool.ToolCallId));
                break;
        }

        foreach (var content in message.Content)
        {
            switch (content)
            {
                case TextContent text:
                    chat.AppendToLastMessage(text.Text);
                    break;
                case ToolCallContent call:
                    chat.AppendToLastMessage($"{call.Name} #{GetShortToolCallId(call.ToolCallId)}\n");
                    break;
                case ToolResultContent result:
                    chat.AppendToLastMessage(result.Results?.ToString() ?? "");
                    break;
            }
        }
    }

    public static async Task DisplayStreamToChatViewAsync(
        IAsyncEnumerable<StreamingUpdate> stream,
        ChatView chat)
    {
        await foreach (var update in stream)
        {
            switch (update)
            {
                case ChatMessageUpdate<AgentMessageDelta> agent:
                    HandleChatDelta(agent.Delta, chat, "Agent");
                    break;
                case ChatMessageUpdate<ToolMessageDelta> tool:
                    HandleChatDelta(tool.Delta, chat, "Tool");
                    break;
                case AIContentUpdate<TextContentDelta> text:
                    HandleAIContentDelta(text.Delta, chat);
                    break;
                case AIContentUpdate<ToolCallContentDelta> toolCall:
                    HandleAIContentDelta(toolCall.Delta, chat);
                    break;
            }
        }
    }

    private static void HandleChatDelta<T>(StreamingOperation<T> delta, ChatView chat, string sender)
        where T : SystemGeneratedMessageDelta
    {
        switch (delta)
        {
            case StartStreamingOperation<T> start:
                if (start.Value is ToolMessageDelta toolDelta)
                {
                    chat.StartMessage(sender, toolDelta.ToolType, GetShortToolCallId(toolDelta.ToolCallId));
                }
                else
                {
                    chat.StartMessage(sender);
                }
                break;

            case SetStreamingOperation<T> set:
                foreach (var content in ((SystemGeneratedMessageDelta)set.Value).Content!)
                    AppendContent(content, chat);
                break;

            case EndStreamingOperation<T>:
                break;
        }
    }

    private static void HandleAIContentDelta<T>(StreamingOperation<T> delta, ChatView chat)
        where T : AIContentDelta
    {
        switch (delta)
        {
            case StartStreamingOperation<T> start when start.Value is ToolCallContentDelta call:
                chat.AppendToLastMessage($"{call.Name} #{GetShortToolCallId(call.ToolCallId)}\n");
                break;

            case AppendStreamingOperation<T> append:
                chat.AppendToLastMessage(append.Value?.ToString() ?? "");
                break;

            case EndStreamingOperation<T>:
                break;
        }
    }

    private static void AppendContent(AIContent content, ChatView chat)
    {
        switch (content)
        {
            case TextContent text:
                chat.AppendToLastMessage(text.Text);
                break;

            case ToolCallContent call:
                chat.AppendToLastMessage($"{call.Name} #{GetShortToolCallId(call.ToolCallId)}\n");
                break;

            case ToolResultContent result:
                // check if results are already strings
                if (result.Results!.GetType() == typeof(string))
                {
                    chat.AppendToLastMessage(result.Results.ToString() ?? "");
                    return;
                }

                // otherwise serialize the results to JSON with out indents
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Allows unescaped characters
                };
                var jsonString = JsonSerializer.Serialize(result.Results, options);
                chat.AppendToLastMessage(jsonString ?? "");
                break;
        }
    }
}
