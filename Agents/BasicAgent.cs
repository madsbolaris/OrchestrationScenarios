using OpenAI.Responses;
using OrchestrationScenarios.Models;

namespace OrchestrationScenarios.Agents;

public class BasicAgent(OpenAIResponseClient client) : Agent(client)
{
}
