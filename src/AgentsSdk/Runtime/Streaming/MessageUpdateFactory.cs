using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Runs.Responses.StreamingOperations;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;

namespace AgentsSdk.Runtime.Streaming;

public static class MessageUpdateFactory
{
    public static ChatMessageUpdate<TDelta> Start<TDelta>(
        string conversationId,
        string messageId)
        where TDelta : SystemGeneratedMessageDelta, new()
    {
        return new ChatMessageUpdate<TDelta>
        {
            ConversationId = conversationId,
            MessageId = messageId,
            Delta = new StartStreamingOperation<TDelta>(new TDelta())
        };
    }

    public static ChatMessageUpdate<TDelta> Start<TDelta>(
        string conversationId,
        string messageId,
        TDelta delta)
        where TDelta : SystemGeneratedMessageDelta, new()
    {
        return new ChatMessageUpdate<TDelta>
        {
            ConversationId = conversationId,
            MessageId = messageId,
            Delta = new StartStreamingOperation<TDelta>(delta)
        };
    }

    public static ChatMessageUpdate<TDelta> Set<TDelta>(
        string conversationId,
        string messageId,
        TDelta delta)
        where TDelta : SystemGeneratedMessageDelta, new()
    {
        return new ChatMessageUpdate<TDelta>
        {
            ConversationId = conversationId,
            MessageId = messageId,
            Delta = new SetStreamingOperation<TDelta>(delta)
        };
    }

    public static ChatMessageUpdate<TDelta> End<TDelta>(
        string conversationId,
        string messageId)
        where TDelta : SystemGeneratedMessageDelta, new()
    {
        return new ChatMessageUpdate<TDelta>
        {
            ConversationId = conversationId,
            MessageId = messageId,
            Delta = new EndStreamingOperation<TDelta>(new TDelta())
        };
    }
}
