// File: Runtime/Streaming/Providers/OpenAI/ToolCallManager.cs

using OrchestrationScenarios.Models.Messages;
using OrchestrationScenarios.Models.Messages.Content;
using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

namespace OrchestrationScenarios.Runtime.Streaming.Providers.OpenAI;

public class ToolCallManager
{
    private bool _processedToolCalls = false;
    private readonly Dictionary<string, ToolCallContent> _builders = [];
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

        _builders[toolCallId] = call;
        return call;
    }

    public void CompleteToolCallArguments(string toolCallId)
    {
        if (!_builders.TryGetValue(toolCallId, out var fnContent))
            return;

        _outputMessages.Add(new AgentMessage
        {
            Content = [fnContent]
        });

        var task = Task.Run(async () => (fnContent, await _invokeFunction(fnContent)));
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
                Delta = new StartStreamingOperation<ToolMessageDelta>(new ToolMessageDelta())
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
