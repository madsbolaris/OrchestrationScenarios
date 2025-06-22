namespace OrchestrationScenarios.Models.Scenarios;

public class ScenarioHeaderAgent
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public List<ScenarioHeaderAgentTool>? Tools { get; set; }
}