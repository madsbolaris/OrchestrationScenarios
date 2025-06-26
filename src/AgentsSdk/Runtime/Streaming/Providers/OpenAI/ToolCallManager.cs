using System.Text;
using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using AgentsSdk.Models.Runs.Responses.StreamingOperations;
using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Messages.Types;
using System.Text.Json;

namespace AgentsSdk.Runtime.Streaming.Providers.OpenAI;

public class ToolCallManager
{
    private bool _processedToolCalls = false;

    // Updated to track both ToolCallContent and its corresponding argument builder
    private readonly Dictionary<string, (ToolCallContent Content, StringBuilder Arguments)> _builders = [];

    private readonly List<Task<(ToolCallContent, object?)>> _tasks = [];

    private readonly Func<ToolCallContent, Task<object?>> _invokeFunction;
    private readonly List<ChatMessage> _outputMessages;

    public ToolCallManager(
        Func<ToolCallContent, Task<object?>> invokeFunction,
        List<ChatMessage> outputMessages)
    {
        _invokeFunction = invokeFunction;
        _outputMessages = outputMessages;
    }

    public ToolCallContent RegisterCall(string toolCallId, string name)
    {
        _processedToolCalls = true;

        var call = new ToolCallContent
        {
            ToolCallId = toolCallId,
            Name = name
        };

        _builders[toolCallId] = (call, new StringBuilder());
        return call;
    }

    public void AddFunctionCallArguments(string toolCallId, string delta)
    {
        if (_builders.TryGetValue(toolCallId, out var tuple))
        {
            tuple.Arguments.Append(delta);
        }
    }

    public void CompleteToolCallArguments(string toolCallId)
    {
        if (!_builders.TryGetValue(toolCallId, out var tuple))
            return;

        tuple.Content.Arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(tuple.Arguments.ToString());

        _outputMessages.Add(new AgentMessage
        {
            Content = [tuple.Content]
        });

        var task = Task.Run(async () => (tuple.Content, await _invokeFunction(tuple.Content)));
        _tasks.Add(task);
    }

    public async IAsyncEnumerable<StreamingUpdate> EmitToolMessagesAsync()
    {
        foreach (var (fnCallContent, toolResults) in await Task.WhenAll(_tasks))
        {
            _outputMessages.Add(new ToolMessage
            {
                ToolCallId = fnCallContent.ToolCallId,
                Content = [new ToolResultContent { Results = toolResults }]
            });

            var messageId = $"m_{fnCallContent.ToolCallId}";

            yield return new ChatMessageUpdate<ToolMessageDelta>
            {
                ConversationId = "", // to be set by caller
                MessageId = messageId,
                Delta = new StartStreamingOperation<ToolMessageDelta>(new ToolMessageDelta()
                {
                    ToolCallId = fnCallContent.ToolCallId
                })
            };

            yield return new ChatMessageUpdate<ToolMessageDelta>
            {
                ConversationId = "",
                MessageId = messageId,
                Delta = new SetStreamingOperation<ToolMessageDelta>(
                    new ToolMessageDelta
                    {
                        Content = [new ToolResultContent { Results = toolResults }]
                    })
            };

            yield return new ChatMessageUpdate<ToolMessageDelta>
            {
                ConversationId = "",
                MessageId = messageId,
                Delta = new EndStreamingOperation<ToolMessageDelta>(new ToolMessageDelta())
            };
        }

        _tasks.Clear();
    }

    public bool HasProcessedToolCalls()
    {
        return _processedToolCalls;
    }
}
