using System.Text.Json.Serialization;
using OrchestrationScenarios.Models.Messages;

namespace OrchestrationScenarios.Models.Runs.Responses.Deltas;

public class ActivityConversationDelta
{
    [JsonPropertyName("m")]
    public List<ChatMessage>? Messages { get; set; }
}
