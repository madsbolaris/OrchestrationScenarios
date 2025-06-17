using OpenAI.Responses;
using OrchestrationScenarios.Models;

namespace OrchestrationScenarios.Agents;

public class WeatherPersonAgent(OpenAIResponseClient client) : Agent(client)
{
}
