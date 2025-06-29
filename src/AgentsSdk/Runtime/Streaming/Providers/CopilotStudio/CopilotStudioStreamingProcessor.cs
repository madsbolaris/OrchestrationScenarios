using System.Text;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Runs.Responses.Deltas.Content;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using AgentsSdk.Models.Messages;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Core.Serialization;
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

        yield return RunUpdateFactory.Start(conversationId, runId);

        await foreach (var update in responseStream)
        {
            if (update.Type is "typing" or "trace")
                continue;

            var messageId = update.Id;
            var contentPartId = $"{messageId}-0";

            switch (update.Type)
            {
                case "message":
                    if (update.Text is null)
                        continue;

                    yield return MessageUpdateFactory.Start<AgentMessageDelta>(conversationId, messageId);
                    yield return AIContentUpdateFactory.Start<TextContentDelta>(contentPartId, 0);
                    yield return AIContentUpdateFactory.Append(contentPartId, 0, new TextContentDelta { Text = update.Text });

                    responseBuilder.Append(update.Text);
                    outputMessages.Add(new AgentMessage
                    {
                        Content = [new TextContent { Text = responseBuilder.ToString() }]
                    });

                    yield return AIContentUpdateFactory.End<TextContentDelta>(contentPartId, 0);
                    yield return MessageUpdateFactory.End<AgentMessageDelta>(conversationId, messageId);
                    break;

                case "event" when update.ValueType == "DynamicPlanStepBindUpdate":
                    var bindData = update.Value.ToJsonElements();
                    await foreach (var u in EmitToolCall(bindData, update.From.Name, conversationId, messageId, contentPartId, outputMessages))
                    {
                        yield return u;
                    }
                    break;

                case "event" when update.ValueType == "DynamicPlanStepFinished":
                    var finishedData = update.Value.ToJsonElements();
                    await foreach (var u in EmitToolResult(finishedData, update.From.Name, conversationId, messageId))
                    {
                        yield return u;
                    }
                    break;

                case "trace":
                case "event":
                case "typing":
                    break;

                default:
                    throw new NotSupportedException($"Unsupported activity type: {update.Type}");
            }
        }

        yield return RunUpdateFactory.End(conversationId, runId);

        // ------------------- Local Functions -------------------

        static string BuildFunctionName(string fromName, string? taskDialogId)
        {
            if (taskDialogId is null) return "Unknown";

            var functionName = taskDialogId;
            if (functionName.StartsWith(fromName, StringComparison.OrdinalIgnoreCase))
                functionName = functionName[(fromName.Length + 1)..];

            if (!functionName.StartsWith("Microsoft.PowerPlatform", StringComparison.OrdinalIgnoreCase))
                functionName = $"Microsoft.PowerPlatform.{functionName}";

            return functionName;
        }

        static async IAsyncEnumerable<StreamingUpdate> EmitToolCall(
            IDictionary<string, JsonElement> elements,
            string fromName,
            string conversationId,
            string messageId,
            string contentPartId,
            List<ChatMessage> outputMessages)
        {
            elements.TryGetValue("stepId", out var stepId);
            elements.TryGetValue("taskDialogId", out var taskDialogId);
            elements.TryGetValue("arguments", out var arguments);

            var argsJson = JsonSerializer.Serialize(arguments);
            var argsDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(argsJson);
            var functionName = BuildFunctionName(fromName, taskDialogId.ToString());

            yield return MessageUpdateFactory.Start<AgentMessageDelta>(conversationId, messageId);
            yield return AIContentUpdateFactory.Start(contentPartId, 0, new ToolCallContentDelta
            {
                ToolCallId = stepId.ToString(),
                Name = functionName
            });
            yield return AIContentUpdateFactory.Append(contentPartId, 0, new ToolCallContentDelta
            {
                Arguments = argsJson
            });

            outputMessages.Add(new AgentMessage
            {
                Content = [new ToolCallContent
                {
                    ToolCallId = stepId.ToString(),
                    Name = taskDialogId.ToString(),
                    Arguments = argsDict!
                }]
            });

            yield return AIContentUpdateFactory.End<ToolCallContentDelta>(contentPartId, 0);
            yield return MessageUpdateFactory.End<AgentMessageDelta>(conversationId, messageId);
            await Task.CompletedTask;
        }

        static async IAsyncEnumerable<StreamingUpdate> EmitToolResult(
            IDictionary<string, JsonElement> elements,
            string fromName,
            string conversationId,
            string messageId)
        {
            elements.TryGetValue("stepId", out var stepId);
            elements.TryGetValue("observation", out var observation);
            elements.TryGetValue("taskDialogId", out var taskDialogId);

            var functionName = BuildFunctionName(fromName, taskDialogId.ToString());

            yield return MessageUpdateFactory.Start(conversationId, messageId, new ToolMessageDelta
            {
                ToolType = functionName,
                ToolCallId = stepId.ToString(),
            });

            yield return MessageUpdateFactory.Set(conversationId, messageId, new ToolMessageDelta
            {
                Content = [new ToolResultContent
                {
                    Results = JsonSerializer.Serialize(observation)
                }]
            });

            yield return MessageUpdateFactory.End<ToolMessageDelta>(conversationId, messageId);
            await Task.CompletedTask;
        }
    }
}
