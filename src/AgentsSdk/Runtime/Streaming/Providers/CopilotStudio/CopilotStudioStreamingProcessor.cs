using System.Text;
using OpenAI.Responses;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Runs.Responses.Deltas.Content;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using AgentsSdk.Models.Messages;
using AgentsSdk.Helpers;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Core.Serialization;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace AgentsSdk.Runtime.Streaming.Providers.CopilotStudio;

public static class CopilotStudioStreamingProcessor
{
    public static async IAsyncEnumerable<StreamingUpdate> ProcessAsync(
        IAsyncEnumerable<IActivity> responseStream,
        string conversationId,
        List<ChatMessage> outputMessages)
    {
        var runId = Guid.NewGuid().ToString();
        var responseBuilder = new StringBuilder();
        string? currentMessageId = null;
        string? currentContentPartId = null;

        yield return RunUpdateFactory.Start(conversationId, runId);

        await foreach (var update in responseStream)
        {
            switch (update.Type)
            {
                case "message":
                    currentMessageId = update.Id;
                    currentContentPartId = $"{currentMessageId}-0";
                    
                    if (update.Text is not null)
                    {
                        yield return MessageUpdateFactory.Start<AgentMessageDelta>(conversationId, currentMessageId);
                        yield return AIContentUpdateFactory.Start<TextContentDelta>(currentContentPartId, 0);
                        yield return AIContentUpdateFactory.Append(currentContentPartId, 0, new TextContentDelta
                        {
                            Text = update.Text
                        });
                        responseBuilder.Append(update.Text);
                        outputMessages.Add(new AgentMessage
                        {
                            Content = [new TextContent { Text = responseBuilder.ToString() }]
                        });
                        yield return AIContentUpdateFactory.End<TextContentDelta>(currentContentPartId, 0);
                        yield return MessageUpdateFactory.End<AgentMessageDelta>(conversationId, currentMessageId);
                    }
                    break;

                case "event":
                    currentMessageId = update.Id;
                    currentContentPartId = $"{currentMessageId}-0";

                    if (update.ValueType == "DynamicPlanStepBindUpdate")
                    {
                        var elements = update.Value.ToJsonElements();
                        elements.TryGetValue("stepId", out var stepId);
                        elements.TryGetValue("taskDialogId", out var taskDialogId);
                        elements.TryGetValue("arguments", out var arguments);

                        var argumentsJson = JsonSerializer.Serialize(arguments);
                        var argumentsDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson);

                        yield return MessageUpdateFactory.Start<AgentMessageDelta>(conversationId, currentMessageId);

                        yield return AIContentUpdateFactory.Start(currentContentPartId, 0, new ToolCallContentDelta
                        {
                            ToolCallId = stepId.ToString(),
                            Name = taskDialogId.ToString()
                        });

                        yield return AIContentUpdateFactory.Append(currentContentPartId, 0, new ToolCallContentDelta
                        {
                            Arguments = argumentsJson
                        });

                        outputMessages.Add(new AgentMessage
                        {
                            Content = [
                                new ToolCallContent
                                {
                                    ToolCallId = stepId.ToString(),
                                    Name = taskDialogId.ToString(),
                                    Arguments = argumentsDict!
                                }
                            ]
                        });

                        yield return AIContentUpdateFactory.End<ToolCallContentDelta>(currentContentPartId, 0);
                        yield return MessageUpdateFactory.End<AgentMessageDelta>(conversationId, currentMessageId);
                    }
                    else if (update.ValueType == "DynamicPlanStepFinished")
                    {
                        var elements = update.Value.ToJsonElements();
                        elements.TryGetValue("stepId", out var stepId);
                        elements.TryGetValue("observation", out var observation);
                        elements.TryGetValue("taskDialogId", out var taskDialogId);

                        yield return MessageUpdateFactory.Start(conversationId, currentMessageId, new ToolMessageDelta
                        {
                            ToolType = taskDialogId.ToString(),
                            ToolCallId = stepId.ToString(),
                        });

                        yield return MessageUpdateFactory.Set(conversationId, currentMessageId, new ToolMessageDelta
                        {
                            Content = [new ToolResultContent
                                {
                                    Results = JsonSerializer.Serialize(observation)
                                }]
                        });
                        yield return MessageUpdateFactory.End<ToolMessageDelta>(conversationId, currentMessageId);
                    }
                    
                    break;

                
                case "trace":
                case "typing":
                    break;
                    
                default:
                    throw new NotSupportedException($"Unsupported activity type: {update.Type}");
            }
        }
        

        yield return RunUpdateFactory.End(conversationId, runId);
    }
}