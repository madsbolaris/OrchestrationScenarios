namespace OrchestrationScenarios.Models.Helpers;

using System.Text;
using OpenAI.Responses;
using OrchestrationScenarios.Models.Messages.Content;
using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.Deltas.Content;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;
using OrchestrationScenarios.Models.Messages;

public static class FromOpenAIResponsesStreamingResponseParser
{
    public static async IAsyncEnumerable<StreamingUpdate> ParseAsync(
        IAsyncEnumerable<StreamingResponseUpdate> responseStream,
        string conversationId,
        Func<ToolCallContent, Task<object?>> invokeFunction,
        List<ChatMessage> outputMessages)
    {
        var functionCallBuilders = new Dictionary<string, ToolCallContent>();
        var functionCallTasks = new List<Task<(ToolCallContent, object?)>>();
        var responseBuilder = new StringBuilder();
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
                                Content = [new ToolCallContent { ToolCallId = fnCall.Id, Name = fnCall.FunctionName }]
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
                            new TextContentDelta { Text = textDelta.Delta })
                    };
                    break;

                case StreamingResponseOutputTextDoneUpdate textDone:
                    outputMessages.Add(new AgentMessage
                    {
                        Content = [new TextContent { Text = responseBuilder.ToString() }]
                    });
                    yield return new AIContentUpdate<TextContentDelta>()
                    {
                        MessageId = textDone.ItemId,
                        Index = textDone.ContentIndex + 1,
                        Delta = new EndStreamingOperation<TextContentDelta>(new TextContentDelta())
                    };
                    responseBuilder.Clear();
                    break;

                case StreamingResponseFunctionCallArgumentsDeltaUpdate fnArgsDelta:
                    yield return new AIContentUpdate<ToolCallContentDelta>()
                    {
                        MessageId = fnArgsDelta.ItemId,
                        Index = fnArgsDelta.OutputIndex,
                        Delta = new AppendStreamingOperation<ToolCallContentDelta>(
                            new ToolCallContentDelta { Arguments = fnArgsDelta.Delta })
                    };
                    break;

                case StreamingResponseFunctionCallArgumentsDoneUpdate fnArgsDone:
                    if (functionCallBuilders.TryGetValue(fnArgsDone.ItemId, out var fnContent))
                    {
                        outputMessages.Add(new AgentMessage
                        {
                            Content = [fnContent]
                        });

                        yield return new AIContentUpdate<ToolCallContentDelta>()
                        {
                            MessageId = fnArgsDone.ItemId,
                            Index = fnArgsDone.OutputIndex + 1,
                            Delta = new EndStreamingOperation<ToolCallContentDelta>(new ToolCallContentDelta())
                        };

                        var task = Task.Run(async () => (fnContent, await invokeFunction(fnContent)));
                        functionCallTasks.Add(task);
                    }
                    break;

                case StreamingResponseOutputItemDoneUpdate itemDone:
                    if (currentMessageId != null)
                    {
                        yield return new ChatMessageUpdate<AgentMessageDelta>()
                        {
                            ConversationId = conversationId,
                            MessageId = itemDone.Item.Id,
                            Delta = new EndStreamingOperation<AgentMessageDelta>(new AgentMessageDelta())
                        };
                        currentMessageId = null;
                    }

                    // Await tool calls and yield tool messages
                    foreach (var task in await Task.WhenAll(functionCallTasks))
                    {
                        var (fnCallContent, toolResults) = task;

                        outputMessages.Add(new ToolMessage
                        {
                            ToolCallId = fnCallContent.ToolCallId,
                            Content = [new ToolResultContent { Results = toolResults }]
                        });

                        yield return new ChatMessageUpdate<ToolMessageDelta>
                        {
                            ConversationId = conversationId,
                            MessageId = "m_" + fnCallContent.ToolCallId,
                            Delta = new StartStreamingOperation<ToolMessageDelta>(new ToolMessageDelta())
                        };

                        yield return new ChatMessageUpdate<ToolMessageDelta>
                        {
                            ConversationId = conversationId,
                            MessageId = "m_" + fnCallContent.ToolCallId,
                            Delta = new SetStreamingOperation<ToolMessageDelta>(new ToolMessageDelta
                            {
                                Content = [new ToolResultContent { Results = toolResults }]
                            })
                        };

                        yield return new ChatMessageUpdate<ToolMessageDelta>
                        {
                            ConversationId = conversationId,
                            MessageId = "m_" + fnCallContent.ToolCallId,
                            Delta = new EndStreamingOperation<ToolMessageDelta>(new ToolMessageDelta())
                        };
                    }
                    functionCallTasks.Clear();
                    break;

                case StreamingResponseWebSearchCallInProgressUpdate webStart:
                    yield return new ChatMessageUpdate<AgentMessageDelta>()
                    {
                        ConversationId = conversationId,
                        MessageId = webStart.ItemId,
                        Delta = new SetStreamingOperation<AgentMessageDelta>(new AgentMessageDelta
                        {
                            Content = [new ToolCallContent { ToolCallId = webStart.ItemId, Name = "WebSearch" }]
                        })
                    };
                    yield return new ChatMessageUpdate<AgentMessageDelta>()
                    {
                        ConversationId = conversationId,
                        MessageId = webStart.ItemId,
                        Delta = new EndStreamingOperation<AgentMessageDelta>(new AgentMessageDelta())
                    };
                    currentMessageId = null;

                    yield return new ChatMessageUpdate<ToolMessageDelta>()
                    {
                        ConversationId = conversationId,
                        MessageId = webStart.ItemId,
                        Delta = new StartStreamingOperation<ToolMessageDelta>(new ToolMessageDelta())
                    };
                    break;

                case StreamingResponseWebSearchCallCompletedUpdate webEnd:
                    outputMessages.Add(new ToolMessage
                    {
                        ToolCallId = webEnd.ItemId,
                        Content = [new ToolResultContent { Results = "REDACTED" }]
                    });
                    yield return new ChatMessageUpdate<ToolMessageDelta>()
                    {
                        ConversationId = conversationId,
                        MessageId = webEnd.ItemId,
                        Delta = new SetStreamingOperation<ToolMessageDelta>(new ToolMessageDelta
                        {
                            Content = [new ToolResultContent { Results = "REDACTED" }]
                        })
                    };
                    yield return new ChatMessageUpdate<ToolMessageDelta>()
                    {
                        ConversationId = conversationId,
                        MessageId = webEnd.ItemId,
                        Delta = new EndStreamingOperation<ToolMessageDelta>(new ToolMessageDelta())
                    };
                    currentMessageId = null;
                    break;
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
