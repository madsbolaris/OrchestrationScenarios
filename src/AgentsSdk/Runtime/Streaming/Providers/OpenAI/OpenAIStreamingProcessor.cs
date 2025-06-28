using System.Text;
using OpenAI.Responses;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Runs.Responses.Deltas.Content;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using AgentsSdk.Models.Messages;
using AgentsSdk.Helpers;
using System.Runtime.CompilerServices;

namespace AgentsSdk.Runtime.Streaming.Providers.OpenAI;

public static class OpenAIStreamingProcessor
{
    public static async IAsyncEnumerable<StreamingUpdate> ProcessAsync(
        IAsyncEnumerable<StreamingResponseUpdate> responseStream,
        string conversationId,
        Func<ToolCallContent, Task<object?>> invokeFunction,
        List<ChatMessage> outputMessages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid().ToString();
        var toolCallManager = new ToolCallManager(invokeFunction, outputMessages);
        var responseBuilder = new StringBuilder();
        string? currentMessageId = null;

        yield return RunUpdateFactory.Start(conversationId, runId);
        var completed = false;

        try
        {
            await foreach (var update in responseStream.WithCancellation(cancellationToken))
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
                            yield return AIContentUpdateFactory.Start(currentMessageId, 0, new ToolCallContentDelta
                            {
                                ToolCallId = fnCall.Id,
                                Name = fnCall.FunctionName
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
                        toolCallManager.AddFunctionCallArguments(fnArgsDelta.ItemId, fnArgsDelta.Delta); // create function
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

                        yield return MessageUpdateFactory.Start(conversationId, webStart.ItemId, new ToolMessageDelta
                        {
                            ToolCallId = webStart.ItemId
                        });
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
            completed = true;
        }
        finally
        {

        }

        if (!toolCallManager.HasProcessedToolCalls() && completed)
        {
            yield return RunUpdateFactory.End(conversationId, runId);
        }
    }
}