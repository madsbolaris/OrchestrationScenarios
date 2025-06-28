using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;

namespace ScenarioRunner.Interfaces;

public interface IScenario
{
    string Name { get; }

    IAsyncEnumerable<StreamingUpdate> RunOpenAIStream(CancellationToken cancellationToken = default);
    IAsyncEnumerable<StreamingUpdate> RunCopilotStudioStream(CancellationToken cancellationToken = default);
    List<ChatMessage> GetStartingMessages();
}
