namespace AgentsSdk.Models.Runs.Responses.Deltas.Content;

public class ToolCallContentDelta : AIContentDelta
{
    public override string Type => "tool_call";

    public string? Arguments { get; set; } = default!;
}
