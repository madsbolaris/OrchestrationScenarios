using System.Text.Json.Serialization;
using AgentsSdk.Models.Runs.Responses.Deltas.Content;

namespace AgentsSdk.Models.Runs.Responses.StreamingUpdates;

/// <summary>
/// Represents a streaming update for an in-progress agent completion.
/// </summary>
public class AIContentUpdate<T> : StreamingUpdate<T> where T : AIContentDelta, new()
{
    [JsonPropertyName("mId")]
    public string MessageId { get; set; } = default!;

    [JsonPropertyName("i")]
    public int Index { get; set; } = default!;
}
