// File: Runtime/Streaming/IStreamingAgentClient.cs
namespace OrchestrationScenarios.Runtime.Streaming;

using OrchestrationScenarios.Models.Agents;
using OrchestrationScenarios.Models.Messages;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

public interface IStreamingAgentClient
{
    IAsyncEnumerable<StreamingUpdate> RunStreamingAsync(Agent agent, List<ChatMessage> messages);
}
