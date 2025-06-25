using AgentsSdk.Helpers;
using AgentsSdk.Models.Agents;
using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Runs.Responses.StreamingOperations;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using AgentsSdk.Runtime.Streaming;

namespace AgentsSdk.Runtime;

public class AgentRunner(IStreamingAgentClient client)
{
    public async Task RunAsync(Agent agent, List<ChatMessage>? allMessages = null)
    {
        allMessages ??= [];
        int agentIndex = allMessages.FindIndex(m => m is AgentMessage);

        var inputMessages = allMessages.Take(agentIndex).ToList();
        var stream = GetStreamingUpdates(agent, inputMessages);

        await ConsoleRenderHelper.DisplayConversationAsync(inputMessages, stream);
    }

    private IAsyncEnumerable<StreamingUpdate> GetStreamingUpdates(
        Agent agent,
        List<ChatMessage> messages)
    {
        return Stream();

        async IAsyncEnumerable<StreamingUpdate> Stream()
        {
            var done = false;

            if (agent.Instructions != null && agent.Instructions.Count > 0)
            {
                messages.InsertRange(0, agent.Instructions);
            }

            while (!done)
            {
                await foreach (var update in client.RunStreamingAsync(agent, messages))
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
