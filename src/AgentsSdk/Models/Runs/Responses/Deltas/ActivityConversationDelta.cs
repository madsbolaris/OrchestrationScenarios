using System.Text.Json.Serialization;
using AgentsSdk.Models.Messages;

namespace AgentsSdk.Models.Runs.Responses.Deltas;

public class ActivityConversationDelta
{
    [JsonPropertyName("m")]
    public List<ChatMessage>? Messages { get; set; }
}
