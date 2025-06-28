using AgentsSdk.Models.Runs.Responses.StreamingUpdates;

namespace ScenarioRunner;

public interface IScenario
{
    string Name { get; }

    // Preferred streaming methods for UI integration
    IAsyncEnumerable<StreamingUpdate> RunOpenAIStream(CancellationToken cancellationToken = default);
    IAsyncEnumerable<StreamingUpdate> RunCopilotStudioStream(CancellationToken cancellationToken = default);
}
