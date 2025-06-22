using Microsoft.Extensions.AI;
using OpenAI.Responses;
using OrchestrationScenarios.Conversion;
using OrchestrationScenarios.Models.Agents;
using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;
using OrchestrationScenarios.Models.Tools.ToolDefinitions.Function;
using OrchestrationScenarios.Parsing;

namespace OrchestrationScenarios.Runtime;

public sealed class ResponseStreamHandler(OpenAIResponseClient client)
{
    public async IAsyncEnumerable<StreamingUpdate> RunStreamingAsync(
        Agent agent,
        List<Models.Messages.ChatMessage> messages)
    {
        var responseItems = messages.SelectMany(ToResponseConverter.Convert).ToList();
        var options = new ResponseCreationOptions { StoredOutputEnabled = true };

        Dictionary<string, AIFunction> aiFunctions = [];

        if (agent.Tools != null)
        {
            foreach (var tool in agent.Tools)
            {
                options.Tools.Add(ToResponseConverter.Convert(tool));

                if (tool is FunctionToolDefinition fnTool)
                {
                    aiFunctions[fnTool.Name] = ToMicrosoftExtensionsAIContentConverter.ToAIFunction(fnTool);
                }
            }
        }

        var response = client.CreateResponseStreamingAsync(responseItems, options);
        var conversationId = messages.FirstOrDefault()?.ConversationId ?? Guid.NewGuid().ToString();

        await foreach (var update in StreamingResponseParser.ParseAsync(
            response,
            conversationId,
            async fn => await FunctionCallExecutor.ExecuteAsync(fn, aiFunctions),
            messages))
        {
            yield return update;
        }
    }
}
