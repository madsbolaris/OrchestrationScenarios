using Microsoft.Extensions.AI;
using OpenAI.Responses;
using OrchestrationScenarios.Conversion;
using OrchestrationScenarios.Models.Agents;
using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;
using OrchestrationScenarios.Models.Tools.ToolDefinitions.Function;
using OrchestrationScenarios.Utilities;
using StreamingUpdate = OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates.StreamingUpdate;

namespace OrchestrationScenarios.Runtime;

public class AgentRunner(OpenAIResponseClient client)
{
    public async Task RunAsync(Agent agent, List<Models.Messages.ChatMessage> allMessages)
    {
        int agentIndex = allMessages.FindIndex(m => m is AgentMessage);
        if (agentIndex == -1)
            throw new InvalidOperationException("No agent message found to stream.");

        var inputMessages = allMessages.Take(agentIndex).ToList();
        var stream = GetStreamingUpdates(agent, inputMessages);

        await ConsoleRenderHelper.DisplayConversationAsync(inputMessages, stream);
    }

    private IAsyncEnumerable<StreamingUpdate> GetStreamingUpdates(Agent agent, List<Models.Messages.ChatMessage> messages)
    {
        List<ResponseTool> responsesTools = [];
        Dictionary<string, AIFunction> aiFunctions = [];

        if (agent.Tools != null)
        {
            responsesTools = [.. agent.Tools.Select(ToResponseConverter.Convert)];

            aiFunctions = agent.Tools
                .OfType<FunctionToolDefinition>()
                .ToDictionary(
                    tool => tool.Name,
                    ToMicrosoftExtensionsAIContentConverter.ToAIFunction
                );
        }

        var handler = new ResponseStreamHandler(client);

        return Stream();
        async IAsyncEnumerable<StreamingUpdate> Stream()
        {
            var done = false;
            while (!done)
            {
                await foreach (var part in handler.RunStreamingAsync(messages, responsesTools, aiFunctions))
                {
                    yield return part;
                    if (part is RunUpdate { Delta: EndStreamingOperation<RunDelta> })
                        done = true;
                }
            }
        }
    }

}
