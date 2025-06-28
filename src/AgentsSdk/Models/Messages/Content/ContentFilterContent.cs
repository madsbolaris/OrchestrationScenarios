using System.Text.Json.Serialization;

namespace AgentsSdk.Models.Messages.Content;

public class ContentFilterContent : AIContent
{
    [JsonPropertyName("type")]
    public override string Type => "content_filter";

    [JsonPropertyName("contentFilter")]
    public string ContentFilter { get; set; } = default!;

    [JsonPropertyName("detected")]
    public bool Detected { get; set; }
}
