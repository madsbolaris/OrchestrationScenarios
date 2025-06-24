namespace OrchestrationScenarios.Models.Tools.ToolDefinitions.BingGrounding;

public class BingGroundingToolDefinition : AgentToolDefinition
{
    public override string Type => "Microsoft.BingGrounding";
    public string ConnectionName { get; set; } = null!;
}
