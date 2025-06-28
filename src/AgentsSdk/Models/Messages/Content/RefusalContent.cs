using System.Text.Json.Serialization;

namespace AgentsSdk.Models.Messages.Content;

public class RefusalContent : AIContent
{
    [JsonPropertyName("type")]
    public override string Type => "refusal";

    [JsonPropertyName("refusal")]
    public string Refusal { get; set; } = default!;
}
