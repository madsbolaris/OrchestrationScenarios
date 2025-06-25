namespace AgentsSdk.Models.Messages.Content;

public class ToolResultContent : AIContent
{
    public override string Type => "tool_result";

    public object? Results { get; set; }
}
