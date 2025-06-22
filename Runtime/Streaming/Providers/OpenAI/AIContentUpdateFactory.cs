// File: Runtime/Streaming/Providers/OpenAI/MessageUpdateFactory.cs

using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.Deltas.Content;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

namespace OrchestrationScenarios.Runtime.Streaming.Providers.OpenAI;

public static class AIContentUpdateFactory
{
    public static AIContentUpdate<TDelta> Start<TDelta>(
        string messageId,
        int index)
        where TDelta : AIContentDelta, new()
    {
        return new AIContentUpdate<TDelta>
        {
            MessageId = messageId,
            Index = index,
            Delta = new StartStreamingOperation<TDelta>(new TDelta())
        };
    }

    public static AIContentUpdate<TDelta> Append<TDelta>(
        string messageId,
        int index,
        TDelta delta)
        where TDelta : AIContentDelta, new()
    {
        return new AIContentUpdate<TDelta>
        {
            MessageId = messageId,
            Index = index,
            Delta = new AppendStreamingOperation<TDelta>(delta)
        };
    }

    public static AIContentUpdate<TDelta> End<TDelta>(
        string messageId,
        int index)
        where TDelta : AIContentDelta, new()
    {
        return new AIContentUpdate<TDelta>
        {
            MessageId = messageId,
            Index = index,
            Delta = new EndStreamingOperation<TDelta>(new TDelta())
        };
    }
}
