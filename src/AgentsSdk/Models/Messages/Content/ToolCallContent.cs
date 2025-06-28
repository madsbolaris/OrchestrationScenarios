namespace AgentsSdk.Models.Messages.Content;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class ToolCallContent : AIContent
{
    [JsonPropertyName("type")]
    public override string Type => "tool_call";

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("toolCallId")]
    public string ToolCallId { get; set; } = default!;

    [JsonPropertyName("arguments")]
    public Dictionary<string, object?>? Arguments { get; set; }
}
