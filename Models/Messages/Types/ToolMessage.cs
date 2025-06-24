namespace OrchestrationScenarios.Models.Messages.Types;

public class ToolMessage : SystemGeneratedMessage
{
    public string ToolType { get; set; } = default!;

    public string ToolCallId { get; set; } = default!;
}
