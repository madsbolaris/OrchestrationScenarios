using System.Text.Json.Serialization;

namespace AgentsSdk.Models.Runs.Responses.Deltas;

public class AgentMessageDelta : SystemGeneratedMessageDelta
{
    [JsonPropertyName("r")]
    public override string Role => MessageDeltaRoles.Agent;

    [JsonPropertyName("aId")]
    public string? AgentId { get; set; }
}
