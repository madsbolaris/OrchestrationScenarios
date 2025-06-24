using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

namespace OrchestrationScenarios.Runtime.Streaming.Providers.OpenAI;

public static class RunUpdateFactory
{
    public static RunUpdate Start(
        string conversationId,
        string runId)
    {
        return new RunUpdate
        {
            RunId = runId,
            ConversationId = conversationId,
            Delta = new StartStreamingOperation<RunDelta>(new RunDelta())
        };
    }

    public static RunUpdate End(
        string conversationId,
        string runId)
    {
        return new RunUpdate
        {
            RunId = runId,
            ConversationId = conversationId,
            Delta = new EndStreamingOperation<RunDelta>(new RunDelta())
        };
    }
}
