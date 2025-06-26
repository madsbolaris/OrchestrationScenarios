namespace AgentsSdk.Models.Runs.Responses.Deltas.Content;

public class ToolCallContentDelta : AIContentDelta
{
    public override string Type => "tool_call";
    public string ToolCallId { get; set; } = default!;
    public string Name { get; set; } = default!;

    public string? Arguments { get; set; } = default!;
}
