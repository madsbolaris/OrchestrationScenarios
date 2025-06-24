namespace OrchestrationScenarios.Models.Messages.Content;

using System.Collections.Generic;

public class ToolCallContent : AIContent
{
    public override string Type => "tool_call";

    public string Name { get; set; } = default!;
    public string ToolCallId { get; set; } = default!;

    public Dictionary<string, object?>? Arguments { get; set; }
}
