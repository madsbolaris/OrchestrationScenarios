using System.Text.Json.Serialization;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;

namespace OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

/// <summary>
/// Represents a streaming update for an in-progress agent completion.
/// </summary>
public class RunUpdate : StreamingUpdate<RunDelta>
{
    [JsonPropertyName("rId")]
    public string RunId { get; set; } = default!;

    [JsonPropertyName("cId")]
    public string ConversationId { get; set; } = default!;

    [JsonPropertyName("u")]
    public CompletionUsage? Usage { get; set; } = default!;
}
