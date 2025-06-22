namespace OrchestrationScenarios.Models.Helpers;

using System.Text;
using OpenAI.Responses;
using OrchestrationScenarios.Models.Messages;
using OrchestrationScenarios.Models.Messages.Content;
using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Runs.Responses;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.Deltas.Content;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

public static class FromOpenAIResponsesStreamingResponseParser
{
    public static async IAsyncEnumerable<StreamingUpdate> ParseAsync(
        IAsyncEnumerable<StreamingResponseUpdate> responseStream,
        List<ChatMessage> messages,
        Dictionary<string, ToolCallContent> functionCallBuilders)
    {
        var responseBuilder = new StringBuilder();

        string conversationId = Guid.NewGuid().ToString();
        string? currentMessageId = null;

        yield return new RunUpdate()
        {
            Delta = new StartStreamingOperation<RunDelta>(new RunDelta())
        };

        await foreach (var update in responseStream)
        {
            switch (update)
            {
                case StreamingResponseOutputItemAddedUpdate outputItem:
                    if (currentMessageId != null && currentMessageId != outputItem.Item.Id)
                    {
                        // If we have a current message and the new item has a different ID, end the current message
                        yield return new ChatMessageUpdate<AgentMessageDelta>()
                        {
                            ConversationId = conversationId,
                            MessageId = currentMessageId,
                            Delta = new EndStreamingOperation<AgentMessageDelta>(new AgentMessageDelta())
                        };
                    }

                    currentMessageId = outputItem.Item.Id!;

                    yield return new ChatMessageUpdate<AgentMessageDelta>()
                    {
                        ConversationId = conversationId,
                        MessageId = currentMessageId,
                        Delta = new StartStreamingOperation<AgentMessageDelta>(new AgentMessageDelta())
                    };

                    if (outputItem.Item is FunctionCallResponseItem fnCall)
                    {
                        var fnCallContent = new ToolCallContent()
                        {
                            ToolCallId = fnCall.Id,
                            Name = fnCall.FunctionName
                        };
                        functionCallBuilders[fnCall.Id] = fnCallContent;

                        yield return new ChatMessageUpdate<AgentMessageDelta>()
                        {
                            ConversationId = conversationId,
                            MessageId = currentMessageId,
                            Delta = new SetStreamingOperation<AgentMessageDelta>(new AgentMessageDelta()
                            {
                                Content = [new ToolCallContent() { ToolCallId = fnCall.Id, Name = fnCall.FunctionName }]
                            })
                        };
                    }

                    break;

                case StreamingResponseContentPartAddedUpdate contentPart:
                    yield return new AIContentUpdate<TextContentDelta>()
                    {
                        MessageId = contentPart.ItemId,
                        Index = contentPart.ContentIndex,
                        Delta = new StartStreamingOperation<TextContentDelta>(new TextContentDelta())
                    };
                    break;

                case StreamingResponseOutputTextDeltaUpdate textDelta:
                    responseBuilder.Append(textDelta.Delta);
                    yield return new AIContentUpdate<TextContentDelta>()
                    {
                        MessageId = textDelta.ItemId,
                        Index = textDelta.ContentIndex,
                        Delta = new AppendStreamingOperation<TextContentDelta>(
                            new TextContentDelta()
                            {
                                Text = textDelta.Delta
                            }
                        )
                    };
                    break;

                case StreamingResponseOutputTextDoneUpdate textDone:
                    messages.Add(new AgentMessage()
                    {
                        Content = [new TextContent() { Text = responseBuilder.ToString() }]
                    });
                    responseBuilder.Clear();

                    yield return new AIContentUpdate<TextContentDelta>()
                    {
                        MessageId = textDone.ItemId,
                        Index = textDone.ContentIndex + 1,
                        Delta = new EndStreamingOperation<TextContentDelta>(new TextContentDelta())
                    };
                    break;

                case StreamingResponseFunctionCallArgumentsDeltaUpdate fnArgsDelta:
                    yield return new AIContentUpdate<ToolCallContentDelta>()
                    {
                        MessageId = fnArgsDelta.ItemId,
                        Index = fnArgsDelta.OutputIndex,
                        Delta = new AppendStreamingOperation<ToolCallContentDelta>(
                            new ToolCallContentDelta()
                            {
                                Arguments = fnArgsDelta.Delta
                            }
                        )
                    };
                    break;

                case StreamingResponseFunctionCallArgumentsDoneUpdate fnArgsDone:
                    if (functionCallBuilders.TryGetValue(fnArgsDone.ItemId, out var fnContent))
                    {
                        messages.Add(new AgentMessage()
                        {
                            Content = [fnContent]
                        });

                        yield return new AIContentUpdate<ToolCallContentDelta>()
                        {
                            MessageId = fnArgsDone.ItemId,
                            Index = fnArgsDone.OutputIndex + 1,
                            Delta = new EndStreamingOperation<ToolCallContentDelta>(new ToolCallContentDelta())
                        };
                    }
                    break;

                case StreamingResponseWebSearchCallInProgressUpdate webStart:
                    // yield return new StreamedStartContent(AuthorRole.Agent, webStart.ItemId);
                    // yield return new StreamedTextContent(AuthorRole.Agent, webStart.ItemId, 0, "WebSearch()");
                    // yield return new StreamedEndContent(AuthorRole.Agent, webStart.ItemId, 1);
                    // yield return new StreamedStartContent(AuthorRole.Tool, "tool-" + webStart.ItemId);
                    break;

                case StreamingResponseWebSearchCallCompletedUpdate webEnd:
                    // yield return new StreamedTextContent(AuthorRole.Tool, "tool-" + webEnd.ItemId, 0, "REDACTED");
                    // yield return new StreamedEndContent(AuthorRole.Tool, "tool-" + webEnd.ItemId, 1);
                    break;


                case StreamingResponseOutputItemDoneUpdate streamingResponseOutputItemDoneUpdate:
                    if (currentMessageId != null)
                    {
                        yield return new ChatMessageUpdate<AgentMessageDelta>()
                        {
                            ConversationId = conversationId,
                            MessageId = streamingResponseOutputItemDoneUpdate.Item.Id,
                            Delta = new EndStreamingOperation<AgentMessageDelta>(new AgentMessageDelta())
                        };
                    }

                    currentMessageId = null;
                    break;

                case StreamingResponseCreatedUpdate:
                case StreamingResponseInProgressUpdate:
                case StreamingResponseCompletedUpdate:
                case StreamingResponseTextAnnotationAddedUpdate:
                case StreamingResponseContentPartDoneUpdate partDone:
                case StreamingResponseWebSearchCallSearchingUpdate:
                    break;

                default:
                    throw new InvalidOperationException($"Unknown response type: {update.GetType().Name}");
            }
        }

        if (functionCallBuilders.Count == 0)
        {
            yield return new RunUpdate()
            {
                Delta = new EndStreamingOperation<RunDelta>(new RunDelta())
            };
        }
    }
}
