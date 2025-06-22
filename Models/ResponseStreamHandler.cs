namespace OrchestrationScenarios.Models;

using Microsoft.Extensions.AI;
using OpenAI.Responses;
using OrchestrationScenarios.Models.Helpers;
using OrchestrationScenarios.Models.Messages.Content;
using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

public sealed class ResponseStreamHandler
{
    private readonly OpenAIResponseClient _client;

    public ResponseStreamHandler(OpenAIResponseClient client)
    {
        _client = client;
    }

    public async IAsyncEnumerable<StreamingUpdate> RunStreamingAsync(List<Messages.ChatMessage> messages, List<ResponseTool> tools, Dictionary<string, AIFunction> aiFunctions)
    {
        var chatMessages = messages.Select(ToMicrosoftExtensionsAIMessageConverter.Convert).ToList();
        var responseItems = chatMessages.SelectMany(ToResponseItemConverter.Convert).ToList();

        var options = new ResponseCreationOptions
        {
            StoredOutputEnabled = true
        };

        foreach (var tool in tools)
        {
            options.Tools.Add(tool);
        }

        var response = _client.CreateResponseStreamingAsync(responseItems, options);

        var functionCallBuilders = new Dictionary<string, ToolCallContent>();

        await foreach (var streamedPart in FromOpenAIResponsesStreamingResponseParser.ParseAsync(
            response,
            messages,
            functionCallBuilders
        ))
        {
            yield return streamedPart;
        }

        if (functionCallBuilders.Count > 0)
        {
            await foreach (var part in HandleFunctionCallsAsync(
                functionCallBuilders,
                messages,
                async fn => await FunctionCallExecutor.ExecuteAsync(fn, aiFunctions)))
                yield return part;
        }
    }

    private async IAsyncEnumerable<StreamingUpdate> HandleFunctionCallsAsync(
        Dictionary<string, ToolCallContent> calls,
        List<Messages.ChatMessage> messages,
        Func<ToolCallContent, Task<object?>> invokeFunction)
    {
        foreach (var functionCallContent in calls.Values)
        {
            var messageId = "m_" + functionCallContent.ToolCallId;

            yield return new ChatMessageUpdate<ToolMessageDelta>()
            {
                Delta = new StartStreamingOperation<ToolMessageDelta>(new ToolMessageDelta()),
            };

            var toolResults = await invokeFunction(functionCallContent);

            messages.Add(new ToolMessage
            {
                ToolCallId = functionCallContent.ToolCallId,
                Content = [
                    new ToolResultContent()
                    {
                        Results = toolResults
                    }
                ]
            });

            yield return new ChatMessageUpdate<ToolMessageDelta>()
            {
                ConversationId = messages.FirstOrDefault()?.ConversationId!,
                MessageId = messageId,
                Delta = new SetStreamingOperation<ToolMessageDelta>(new ToolMessageDelta()
                {
                    Content = [
                        new ToolResultContent()
                        {
                            Results = toolResults
                        }
                    ]
                })
            };

            yield return new ChatMessageUpdate<ToolMessageDelta>()
            {
                ConversationId = messages.FirstOrDefault()?.ConversationId!,
                MessageId = messageId!,
                Delta = new EndStreamingOperation<ToolMessageDelta>(new ToolMessageDelta())
            };
        }
    }
}
