namespace AgentsSdk.Runtime.Streaming;

using AgentsSdk.Models.Agents;
using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;

public interface IStreamingAgentClient
{
    IAsyncEnumerable<StreamingUpdate> RunStreamingAsync(Agent agent, List<ChatMessage> messages, 
        CancellationToken cancellationToken = default);
}
