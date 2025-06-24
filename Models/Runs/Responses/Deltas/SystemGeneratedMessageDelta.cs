using OrchestrationScenarios.Models.Messages.Content;
using System.Text.Json.Serialization;

namespace OrchestrationScenarios.Models.Runs.Responses.Deltas;

public abstract class SystemGeneratedMessageDelta
{
    [JsonPropertyName("r")]
    public abstract string Role { get; }

    [JsonPropertyName("rId")]
    public string? RunId { get; set; }

    [JsonPropertyName("c")]
    public List<AIContent>? Content { get; set; }

    // TODO: Add once Assistants API has this information
    // [JsonPropertyName("n")]
    // public string? AuthorName { get; set; }

    public long? CreatedAt { get; set; }
    public long? CompletedAt { get; set; }
    public CompletionUsage? Usage { get; set; } = default!;
}
