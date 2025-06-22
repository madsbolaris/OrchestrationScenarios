using OpenAI.Responses;
using OrchestrationScenarios.Models.Agents;
using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;
using OrchestrationScenarios.Runtime.Response;
using OrchestrationScenarios.Utilities;

namespace OrchestrationScenarios.Runtime;

public class AgentRunner(ResponseStreamHandler handler)
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

    private IAsyncEnumerable<StreamingUpdate> GetStreamingUpdates(
        Agent agent,
        List<Models.Messages.ChatMessage> messages)
    {
        return Stream();

        async IAsyncEnumerable<StreamingUpdate> Stream()
        {
            var done = false;

            while (!done)
            {
                await foreach (var update in handler.RunStreamingAsync(agent, messages))
                {
                    yield return update;

                    if (update is RunUpdate { Delta: EndStreamingOperation<RunDelta> })
                    {
                        done = true;
                        break;
                    }
                }
            }
        }
    }
}
