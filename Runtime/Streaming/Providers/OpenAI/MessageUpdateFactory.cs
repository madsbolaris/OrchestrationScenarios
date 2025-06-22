// File: Runtime/Streaming/Providers/OpenAI/MessageUpdateFactory.cs

using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.Deltas.Content;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

namespace OrchestrationScenarios.Runtime.Streaming.Providers.OpenAI;

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
