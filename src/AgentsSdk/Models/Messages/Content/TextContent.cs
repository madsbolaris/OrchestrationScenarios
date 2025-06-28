namespace AgentsSdk.Models.Messages.Content;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class TextContent : AIContent
{
    [JsonPropertyName("type")]
    public override string Type => "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = default!;

    [JsonPropertyName("annotations")]
    public List<Annotation> Annotations { get; set; } = [];
}
