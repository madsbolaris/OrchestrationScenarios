using OrchestrationScenarios.Models;

namespace OrchestrationScenarios.Agents;

public class WeatherPersonAgent : Agent
{
    public override Task<string> ChatAsync(string input)
    {
        return Task.FromResult("Today's forecast: 72°F and sunny.");
    }
}
