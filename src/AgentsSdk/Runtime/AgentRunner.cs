using System.Runtime.CompilerServices;
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
    public IAsyncEnumerable<StreamingUpdate> RunAsync(Agent agent, List<ChatMessage> chatHistory, CancellationToken cancellationToken = default)
    {
        return Stream(cancellationToken);

        async IAsyncEnumerable<StreamingUpdate> Stream(
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var prepended = false;

            try
            {
                if (agent.Instructions != null && agent.Instructions.Count > 0)
                {
                    chatHistory.InsertRange(0, agent.Instructions);
                    prepended = true;
                }

                var done = false;

                while (!done)
                {
                    await foreach (var update in client.RunStreamingAsync(agent, chatHistory, cancellationToken))
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
                    chatHistory.RemoveRange(0, agent.Instructions!.Count);
                }
            }
        }
    }
}
