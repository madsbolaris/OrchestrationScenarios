namespace AgentsSdk.Models.Runs.Responses.Deltas.Content;

public class ToolResultContentDelta : AIContentDelta
{
    public override string Type => "tool_result";

    public string? Result { get; set; } = default!;
}
