using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text;
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

	private static readonly Dictionary<Type, StringBuilder> _openXmlBuffers = new();

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
				chat.MessageHistory.AppendXml(XmlChatSerializer.SerializeStartTag(typeof(UserMessage)));
				break;
			case AgentMessage:
				chat.StartMessage("Agent");
				chat.MessageHistory.AppendXml(XmlChatSerializer.SerializeStartTag(typeof(AgentMessage)));
				break;
			case ToolMessage tool:
				var shortId = GetShortToolCallId(tool.ToolCallId);
				chat.StartMessage("Tool", tool.ToolType, shortId);
				chat.MessageHistory.AppendXml(XmlChatSerializer.SerializeStartTag(
					typeof(ToolMessage), new Dictionary<string, string>
					{
						["for"] = shortId
					}));
				break;
		}

		foreach (var content in message.Content)
		{
			AppendContent(content, chat);
		}

		chat.MessageHistory.AppendXml(XmlChatSerializer.SerializeEndTag(GetMessageType(message)));
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
					HandleChatDelta(agent.Delta, chat, typeof(AgentMessageDelta));
					break;
				case ChatMessageUpdate<ToolMessageDelta> tool:
					HandleChatDelta(tool.Delta, chat, typeof(ToolMessageDelta));
					break;
				case AIContentUpdate<TextContentDelta> text:
					HandleAIContentDelta(text.Delta, chat, typeof(TextContentDelta));
					break;
				case AIContentUpdate<ToolCallContentDelta> toolCall:
					HandleAIContentDelta(toolCall.Delta, chat, typeof(ToolCallContentDelta));
					break;
			}
		}
	}

	private static void HandleChatDelta<T>(StreamingOperation<T> delta, ChatView chat, Type type)
		where T : SystemGeneratedMessageDelta
	{
		switch (delta)
		{
			case StartStreamingOperation<T> start:
				if (start.Value is ToolMessageDelta toolDelta)
				{
					var shortId = GetShortToolCallId(toolDelta.ToolCallId);
					chat.StartMessage("Tool", toolDelta.ToolType, shortId);
					chat.MessageHistory.AppendXml(XmlChatSerializer.SerializeStartTag(
						type, new Dictionary<string, string>
						{
							["for"] = shortId
						}));
				}
				else
				{
					chat.StartMessage("Agent");
					chat.MessageHistory.AppendXml(XmlChatSerializer.SerializeStartTag(type));
				}
				break;

			case SetStreamingOperation<T> set:
				foreach (var content in ((SystemGeneratedMessageDelta)set.Value).Content!)
				{
					AppendContent(content, chat);
				}
				break;

			case EndStreamingOperation<T> end:
				chat.MessageHistory.AppendXml(XmlChatSerializer.SerializeEndTag(type));
				break;
		}
	}

	private static void HandleAIContentDelta<T>(StreamingOperation<T> delta, ChatView chat, Type type)
		where T : AIContentDelta
	{
		switch (delta)
		{
			case StartStreamingOperation<T> start:
				if (start.Value is ToolCallContentDelta toolCall)
				{
					var shortId = GetShortToolCallId(toolCall.ToolCallId);
					chat.AppendToLastMessage($"{toolCall.Name} #{shortId}\n");

					_openXmlBuffers[type] = new StringBuilder();
					_openXmlBuffers[type].Append(XmlChatSerializer.SerializeStartTag(
						type, new Dictionary<string, string>
						{
							["name"] = toolCall.Name,
							["id"] = shortId
						}));
				}
				else
				{
					_openXmlBuffers[type] = new StringBuilder();
					_openXmlBuffers[type].Append(XmlChatSerializer.SerializeStartTag(type));
				}
				break;

			case AppendStreamingOperation<T> append:
				var token = append.Value?.ToString() ?? "";
				chat.AppendToLastMessage(token);

				if (_openXmlBuffers.TryGetValue(type, out var buf))
					buf.Append(XmlChatSerializer.Escape(token));
				break;

			case EndStreamingOperation<T>:
				if (_openXmlBuffers.TryGetValue(type, out var completed))
				{
					completed.Append(XmlChatSerializer.SerializeEndTag(type));
					chat.MessageHistory.AppendXml(completed.ToString());
					_openXmlBuffers.Remove(type);
				}
				break;
		}
	}

	private static void AppendContent(AIContent content, ChatView chat)
	{
		chat.AppendToLastMessage(content switch
		{
			TextContent text => text.Text,
			ToolCallContent call => $"{call.Name} #{GetShortToolCallId(call.ToolCallId)}\n",
			ToolResultContent result => result.Results is string s
				? s
				: JsonSerializer.Serialize(result.Results, new JsonSerializerOptions
				{
					WriteIndented = false,
					Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
			 }),
			_ => ""
		});

		chat.MessageHistory.AppendXml(XmlChatSerializer.SerializeContent(content));
	}

	private static Type GetMessageType(ChatMessage message) => message switch
	{
		UserMessage => typeof(UserMessage),
		AgentMessage => typeof(AgentMessage),
		ToolMessage => typeof(ToolMessage),
		_ => typeof(object)
	};
}
