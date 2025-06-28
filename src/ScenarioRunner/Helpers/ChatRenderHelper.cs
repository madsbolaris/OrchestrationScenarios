using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Runs.Responses.Deltas.Content;
using AgentsSdk.Models.Runs.Responses.StreamingOperations;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using Common.Views;
using ScenarioRunner.Views;

namespace ScenarioRunner.Helpers;

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

	public static async Task DisplayStreamToChatViewAsync(IAsyncEnumerable<StreamingUpdate> stream, ChatView chat)
	{
		await foreach (var update in stream)
		{
			switch (update)
			{
				case ChatMessageUpdate<AgentMessageDelta> chatDelta:
					HandleChatDelta(chatDelta.Delta, chat, "Bot");
					break;

				case ChatMessageUpdate<ToolMessageDelta> toolDelta:
					HandleChatDelta(toolDelta.Delta, chat, "Tool");
					break;

				case AIContentUpdate<TextContentDelta> textDelta:
					HandleAIContentDelta(textDelta.Delta, chat);
					break;

				case AIContentUpdate<ToolCallContentDelta> toolCallDelta:
					HandleAIContentDelta(toolCallDelta.Delta, chat);
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
                if (start.Value is ToolMessageDelta toolMessageDelta)
                {
                    chat.StartMessage(sender, toolName: toolMessageDelta.ToolType, toolCallId: GetShortToolCallId(toolMessageDelta.ToolCallId));
                }
                else
                {
                    chat.StartMessage(sender);
                }
                break;

			case SetStreamingOperation<T> set:
				foreach (var content in ((SystemGeneratedMessageDelta)set.Value).Content!)
				{
					AppendContent(content, chat);
				}
				break;

			case EndStreamingOperation<T>:
				break;
		}
	}

	private static void HandleAIContentDelta<T>(StreamingOperation<T> delta, ChatView chat) where T : AIContentDelta
    {
        switch (delta)
        {
            case StartStreamingOperation<T> start:
                if (start.Value is ToolCallContentDelta call)
                {
                    var shortId = GetShortToolCallId(call.ToolCallId);
                    var header = $"{call.Name} #{shortId}\n";
                    chat.AppendToLastMessage(header);
                }
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
				chat.AppendToLastMessage(call.Arguments?.ToString() ?? "");
				break;

			case ToolResultContent result:
				chat.AppendToLastMessage(result.Results?.ToString() ?? "");
				break;
		}
	}
}
