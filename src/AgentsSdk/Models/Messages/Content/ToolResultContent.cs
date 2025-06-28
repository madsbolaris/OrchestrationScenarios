using System.Text.Json.Serialization;

namespace AgentsSdk.Models.Messages.Content;

public class ToolResultContent : AIContent
{
    [JsonPropertyName("type")]
    public override string Type => "tool_result";

    [JsonPropertyName("toolCallId")]
    public object? Results { get; set; }
}
