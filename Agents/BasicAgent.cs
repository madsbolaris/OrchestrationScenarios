using OrchestrationScenarios.Models;

namespace OrchestrationScenarios.Agents;

public class BasicAgent : Agent
{
    public override Task<string> ChatAsync(string input)
    {
        return Task.FromResult($"[BasicAgent using {Model} responds to: {input}]");
    }
}
