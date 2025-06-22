using OpenAI.Responses;
using OrchestrationScenarios.Agents;
using OrchestrationScenarios.Models.Messages;
using OrchestrationScenarios.Models.Messages.Content;
using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Runtime;

namespace OrchestrationScenarios.Scenarios;

public class BasicChatScenario(AgentRunner runner, BasicAgent agent) : IScenario
{
    public string Name => "Basic Chat Scenario";

    public async Task RunAsync()
    {
        List<ChatMessage> messages = [
            new UserMessage()
            {
                // Content = [new TextContent() {Text = "What's today's date?"}]
                Content = [new TextContent() {Text = "what's today's date? Then search for today's weather in Seattle, WA."}]
            },
            new AgentMessage()
            {
                Content = [new ToolCallContent() { Name = "DateTimeNow", ToolCallId = "0001", Arguments = [] }]
            },
            new ToolMessage()
            {
                ToolCallId = "0001",
                Content = [new ToolResultContent() { Results = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }]
            },
            new AgentMessage()
            {
                Content = [new ToolCallContent() { Name = "WebSearch", ToolCallId = "0002", Arguments = new Dictionary<string, object?> { { "query", "weather in Seattle, WA" } } }]
            },
            new ToolMessage()
            {
                ToolCallId = "0002",
                Content = [new ToolResultContent() { Results = "REDACTED" }]
            },
            new AgentMessage()
            {
                Content = [new TextContent() { Text = """
                Jun 19, 2025, 1:49:37 PM

                ## Weather for Seattle, WA:
                Current Conditions: Mostly sunny, 64°F (18°C)

                Daily Forecast:
                * Thursday, June 19: Low: 51°F (11°C), High: 67°F (19°C), Description: Times of clouds and sun
                * Friday, June 20: Low: 52°F (11°C), High: 60°F (16°C), Description: Mostly cloudy with a couple of showers
                * Saturday, June 21: Low: 55°F (13°C), High: 62°F (17°C), Description: Cloudy and cool with a couple of showers
                * Sunday, June 22: Low: 54°F (12°C), High: 71°F (22°C), Description: Occasional rain and drizzle in the morning; otherwise, mostly cloudy and warmer
                * Monday, June 23: Low: 56°F (13°C), High: 76°F (24°C), Description: Abundant sunshine and pleasant
                * Tuesday, June 24: Low: 58°F (14°C), High: 83°F (28°C), Description: Brilliant sunshine and very warm
                * Wednesday, June 25: Low: 57°F (14°C), High: 76°F (24°C), Description: Cloudy


                After an unusually dry spring and hotter-than-normal June, rain is finally returning to Seattle, providing a much-needed reprieve from increasing fire risks, depleted snowpacks, and widespread drought conditions. The National Weather Service forecasts rain and showers in the region from Wednesday through Sunday. ([axios.com](https://www.axios.com/local/seattle/2025/06/17/seattle-rain-returns-fire-risk-drought?utm_source=openai))
                """ } ]
            }
        ];

        await runner.RunAsync(agent, messages);
    }
}
