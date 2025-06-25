using System.Text.Json.Serialization;
using AgentsSdk.Models.Messages;

namespace AgentsSdk.Models.Runs.Responses.Deltas;

public class RunDelta
{
    [JsonPropertyName("aId")]
    public string? AgentId { get; set; }

    public long? CreatedAt { get; set; }

    public long? CompletedAt { get; set; }

    [JsonPropertyName("s")]
    public RunStatus? Status { get; set; }

    [JsonPropertyName("o")]
    public List<ChatMessage>? Output { get; set; }

    [JsonPropertyName("u")]
    public CompletionUsage? Usage { get; set; }

    public IncompleteDetails? IncompleteDetails { get; set; }
}
