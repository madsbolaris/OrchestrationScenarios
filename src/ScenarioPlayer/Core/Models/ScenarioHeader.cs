using AgentsSdk.Models.Agents;

namespace ScenarioPlayer.Core.Models;

public class ScenarioHeader
{
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Agent Agent { get; set; } = new();
}
