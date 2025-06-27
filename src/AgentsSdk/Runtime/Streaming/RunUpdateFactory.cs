using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Runs.Responses.StreamingOperations;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;

namespace AgentsSdk.Runtime.Streaming;

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
