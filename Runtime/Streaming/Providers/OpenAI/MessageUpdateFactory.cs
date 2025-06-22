// File: Runtime/Streaming/Providers/OpenAI/MessageUpdateFactory.cs

using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.Deltas.Content;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

namespace OrchestrationScenarios.Runtime.Streaming.Providers.OpenAI;

public static class MessageUpdateFactory
{
    public static RunUpdate StartRun(
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

    public static RunUpdate EndRun(
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

    public static ChatMessageUpdate<TDelta> StartMessage<TDelta>(
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

    public static ChatMessageUpdate<TDelta> SetMessage<TDelta>(
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

    public static ChatMessageUpdate<TDelta> EndMessage<TDelta>(
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

    public static AIContentUpdate<TDelta> StartContent<TDelta>(
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

    public static AIContentUpdate<TDelta> AppendContent<TDelta>(
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

    public static AIContentUpdate<TDelta> EndContent<TDelta>(
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
