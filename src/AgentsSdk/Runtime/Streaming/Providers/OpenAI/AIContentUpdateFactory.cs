using AgentsSdk.Models.Runs.Responses.Deltas.Content;
using AgentsSdk.Models.Runs.Responses.StreamingOperations;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;

namespace AgentsSdk.Runtime.Streaming.Providers.OpenAI;

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

    public static AIContentUpdate<TDelta> Start<TDelta>(
        string messageId,
        int index,
        TDelta delta)
        where TDelta : AIContentDelta, new()
    {
        return new AIContentUpdate<TDelta>
        {
            MessageId = messageId,
            Index = index,
            Delta = new StartStreamingOperation<TDelta>(delta)
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
