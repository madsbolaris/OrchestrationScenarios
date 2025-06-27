using AgentsSdk.Helpers;
using AgentsSdk.Models.Agents;
using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Runs.Responses.StreamingOperations;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using AgentsSdk.Runtime.Streaming;

namespace AgentsSdk.Runtime;

public class AgentRunner<T>(T client) where T : IStreamingAgentClient
{
    public async Task RunAsync(Agent agent, List<ChatMessage> allMessages)
    {
        var stream = GetStreamingUpdates(agent, allMessages);
        await ConsoleRenderHelper.DisplayStreamAsync(stream);
    }

    private IAsyncEnumerable<StreamingUpdate> GetStreamingUpdates(
        Agent agent,
        List<ChatMessage> messages)
    {
        return Stream();

        async IAsyncEnumerable<StreamingUpdate> Stream()
        {
            var prepended = false;

            try
            {
                if (agent.Instructions != null && agent.Instructions.Count > 0)
                {
                    messages.InsertRange(0, agent.Instructions);
                    prepended = true;
                }

                var done = false;

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
            finally
            {
                if (prepended)
                {
                    messages.RemoveRange(0, agent.Instructions!.Count);
                }
            }
        }
    }
}
