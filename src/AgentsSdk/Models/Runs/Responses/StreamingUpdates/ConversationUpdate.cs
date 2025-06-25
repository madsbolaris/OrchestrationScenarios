using System.Text.Json.Serialization;
using AgentsSdk.Models.Conversations;

namespace AgentsSdk.Models.Runs.Responses.StreamingUpdates;

/// <summary>
/// Represents a streaming update for an in-progress agent completion.
/// </summary>
public class ConversationUpdate : StreamingUpdate<Conversation>
{
    [JsonPropertyName("tId")]
    public string ConversationId { get; set; } = default!;

    [JsonPropertyName("u")]
    public CompletionUsage? Usage { get; set; } = default!;
}
