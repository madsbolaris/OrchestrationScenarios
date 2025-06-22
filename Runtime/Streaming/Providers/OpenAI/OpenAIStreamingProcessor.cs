// File: Runtime/Streaming/Providers/OpenAI/OpenAIStreamingProcessor.cs

using System.Text;
using OpenAI.Responses;
using OrchestrationScenarios.Models.Messages.Content;
using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.Deltas.Content;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;
using OrchestrationScenarios.Models.Messages;

namespace OrchestrationScenarios.Runtime.Streaming.Providers.OpenAI;

public static class OpenAIStreamingProcessor
{
    public static async IAsyncEnumerable<StreamingUpdate> ProcessAsync(
        IAsyncEnumerable<StreamingResponseUpdate> responseStream,
        string conversationId,
        Func<ToolCallContent, Task<object?>> invokeFunction,
        List<ChatMessage> outputMessages)
    {
        var runId = Guid.NewGuid().ToString();
        var toolCallManager = new ToolCallManager(invokeFunction, outputMessages);
        var responseBuilder = new StringBuilder();
        string? currentMessageId = null;

        yield return RunUpdateFactory.Start(conversationId, runId);

        await foreach (var update in responseStream)
        {
            switch (update)
            {
                case StreamingResponseOutputItemAddedUpdate outputItem:
                    if (currentMessageId != null && currentMessageId != outputItem.Item.Id)
                    {
                        yield return MessageUpdateFactory.End<AgentMessageDelta>(conversationId, currentMessageId);
                    }

                    currentMessageId = outputItem.Item.Id!;
                    yield return MessageUpdateFactory.Start<AgentMessageDelta>(conversationId, currentMessageId);

                    if (outputItem.Item is FunctionCallResponseItem fnCall)
                    {
                        var fnCallContent = toolCallManager.RegisterCall(fnCall.Id, fnCall.FunctionName);
                        yield return MessageUpdateFactory.Set(conversationId, currentMessageId, new AgentMessageDelta
                        {
                            Content = [fnCallContent]
                        });
                    }
                    break;

                case StreamingResponseContentPartAddedUpdate contentPart:
                    yield return AIContentUpdateFactory.Start<TextContentDelta>(contentPart.ItemId, contentPart.ContentIndex);
                    break;

                case StreamingResponseOutputTextDeltaUpdate textDelta:
                    responseBuilder.Append(textDelta.Delta);
                    yield return AIContentUpdateFactory.Append(textDelta.ItemId, textDelta.ContentIndex, new TextContentDelta
                    {
                        Text = textDelta.Delta
                    });
                    break;

                case StreamingResponseOutputTextDoneUpdate textDone:
                    outputMessages.Add(new AgentMessage
                    {
                        Content = [new TextContent { Text = responseBuilder.ToString() }]
                    });
                    yield return AIContentUpdateFactory.End<TextContentDelta>(textDone.ItemId, textDone.ContentIndex + 1);
                    responseBuilder.Clear();
                    break;

                case StreamingResponseFunctionCallArgumentsDeltaUpdate fnArgsDelta:
                    yield return AIContentUpdateFactory.Append(fnArgsDelta.ItemId, fnArgsDelta.OutputIndex, new ToolCallContentDelta
                    {
                        Arguments = fnArgsDelta.Delta
                    });
                    break;

                case StreamingResponseFunctionCallArgumentsDoneUpdate fnArgsDone:
                    toolCallManager.CompleteToolCallArguments(fnArgsDone.ItemId);
                    yield return AIContentUpdateFactory.End<ToolCallContentDelta>(fnArgsDone.ItemId, fnArgsDone.OutputIndex + 1);
                    break;

                case StreamingResponseOutputItemDoneUpdate itemDone:
                    if (currentMessageId != null)
                    {
                        yield return MessageUpdateFactory.End<AgentMessageDelta>(conversationId, itemDone.Item.Id);
                        currentMessageId = null;
                    }

                    await foreach (var toolUpdate in toolCallManager.EmitToolMessagesAsync())
                    {
                        if (toolUpdate is ChatMessageUpdate<ToolMessageDelta> chatMessageUpdate)
                        {
                            chatMessageUpdate.ConversationId = conversationId;
                        }
                        yield return toolUpdate;
                    }
                    break;

                case StreamingResponseWebSearchCallInProgressUpdate webStart:
                    yield return MessageUpdateFactory.Set<AgentMessageDelta>(conversationId, webStart.ItemId, new AgentMessageDelta
                    {
                        Content = [new ToolCallContent { ToolCallId = webStart.ItemId, Name = "Microsoft.BingGrounding.Search" }]
                    });
                    yield return MessageUpdateFactory.End<AgentMessageDelta>(conversationId, webStart.ItemId);
                    currentMessageId = null;

                    yield return MessageUpdateFactory.Start<ToolMessageDelta>(conversationId, webStart.ItemId);
                    break;

                case StreamingResponseWebSearchCallCompletedUpdate webEnd:
                    var results = new ToolResultContent { Results = "REDACTED" };
                    outputMessages.Add(new ToolMessage
                    {
                        ToolCallId = webEnd.ItemId,
                        Content = [results]
                    });
                    yield return MessageUpdateFactory.Set(conversationId, webEnd.ItemId, new ToolMessageDelta
                    {
                        Content = [results]
                    });
                    yield return MessageUpdateFactory.End<ToolMessageDelta>(conversationId, webEnd.ItemId);
                    currentMessageId = null;
                    break;
            }
        }

        if (!toolCallManager.HasProcessedToolCalls())
        {
            yield return RunUpdateFactory.End(conversationId, runId);
        }
    }
}