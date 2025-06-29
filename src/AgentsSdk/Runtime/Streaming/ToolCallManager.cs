using System.Text;
using System.Text.Json;
using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using AgentsSdk.Models.Runs.Responses.StreamingOperations;
using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Tools;
using System.Text.Json.Nodes;

namespace AgentsSdk.Runtime.Streaming;

internal class ToolCallManager
{
    private bool _processedToolCalls = false;

    private readonly Dictionary<string, (ToolCallContent Content, StringBuilder Arguments)> _builders = [];
    private readonly List<Task<(ToolCallContent, object?)>> _tasks = [];

    private readonly Dictionary<string, ToolMetadata> _toolMetadata;
    private readonly List<ChatMessage> _outputMessages;

    public ToolCallManager(
        Dictionary<string, ToolMetadata> toolMetadata,
        List<ChatMessage> outputMessages)
    {
        _toolMetadata = toolMetadata;
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

        // Deserialize raw string buffer into arguments dictionary
        var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(tuple.Arguments.ToString())!;
        tuple.Content.Arguments = arguments;

        _outputMessages.Add(new AgentMessage
        {
            Content = [tuple.Content]
        });

        var metadata = _toolMetadata.GetValueOrDefault(tuple.Content.Name!)
            ?? throw new InvalidOperationException($"Tool '{tuple.Content.Name}' not found.");

        if (metadata.Executor is null)
            throw new InvalidOperationException($"Tool '{metadata.Name}' is not executable.");

        // Just pass the arguments directly
        var task = Task.Run(async () => (tuple.Content, await metadata.Executor(arguments)));

        _tasks.Add(task);
    }


    public async IAsyncEnumerable<StreamingUpdate> EmitToolMessagesAsync(string conversationId)
    {
        foreach (var (fnCallContent, toolResults) in await Task.WhenAll(_tasks))
        {
            var metadata = _toolMetadata.GetValueOrDefault(fnCallContent.Name!)
                ?? throw new InvalidOperationException($"Tool '{fnCallContent.Name}' not found.");

            _outputMessages.Add(new ToolMessage
            {
                ToolType = metadata.Type,
                ToolCallId = fnCallContent.ToolCallId,
                Content = [new ToolResultContent { Results = toolResults }]
            });

            var messageId = $"m_{fnCallContent.ToolCallId}";

            yield return new ChatMessageUpdate<ToolMessageDelta>
            {
                ConversationId = conversationId,
                MessageId = messageId,
                Delta = new StartStreamingOperation<ToolMessageDelta>(new ToolMessageDelta
                {
                    ToolType = metadata.Type,
                    ToolCallId = fnCallContent.ToolCallId
                })
            };

            yield return new ChatMessageUpdate<ToolMessageDelta>
            {
                ConversationId = conversationId,
                MessageId = messageId,
                Delta = new SetStreamingOperation<ToolMessageDelta>(
                    new ToolMessageDelta
                    {
                        Content = [new ToolResultContent { Results = toolResults }]
                    })
            };

            yield return new ChatMessageUpdate<ToolMessageDelta>
            {
                ConversationId = conversationId,
                MessageId = messageId,
                Delta = new EndStreamingOperation<ToolMessageDelta>(new ToolMessageDelta())
            };
        }

        _tasks.Clear();
    }

    public bool HasProcessedToolCalls() => _processedToolCalls;
}
