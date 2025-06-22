namespace OrchestrationScenarios.Models.Scenarios;

public class ScenarioHeader
{
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ScenarioHeaderAgent Agent { get; set; } = new();
}