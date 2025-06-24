namespace OrchestrationScenarios.Models.Agents.ToolChoiceBehaviors;

/// <summary>
/// Automatically selects one or more tools from the provided list.
/// </summary>
public class RequiredToolChoiceBehavior : ToolChoiceBehavior
{
    public override string Type => "auto";

    public List<string> ToolNames { get; set; } = [];
}
