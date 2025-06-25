using System.Text.Json.Serialization;
using AgentsSdk.Models.Runs.Responses.Deltas;

namespace AgentsSdk.Models.Runs.Responses.StreamingUpdates;

/// <summary>
/// Represents a streaming update for an in-progress agent completion.
/// </summary>
public class ChatMessageUpdate<T> : StreamingUpdate<T> where T : SystemGeneratedMessageDelta, new()
{
    [JsonPropertyName("mId")]
    public string MessageId { get; set; } = default!;

    [JsonPropertyName("cId")]
    public string ConversationId { get; set; } = default!;

    [JsonPropertyName("u")]
    public CompletionUsage? Usage { get; set; } = default!;
}
