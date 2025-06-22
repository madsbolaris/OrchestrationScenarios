using OrchestrationScenarios.Models.Agents;
using OrchestrationScenarios.Models.Agents.Models.OpenAI;
using OrchestrationScenarios.Models.Messages;
using OrchestrationScenarios.Models.Messages.Content;
using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Tools.ToolDefinitions.BingGrounding;
using OrchestrationScenarios.Models.Tools.ToolDefinitions.Function;

namespace OrchestrationScenarios.Agents;

public class BasicAgent : Agent
{
    public BasicAgent()
    {
        DisplayName = "Basic Agent";
        Description = "A simple agent that provides the current time.";
        Model = new OpenAIAgentModel()
        {
            Id = "gpt-4o"
        };
        Instructions = new List<ChatMessage>
        {
            new SystemMessage
            {
                Content = [new TextContent(){Text = "You are a helpful assistant that provides the current time when asked."}]
            }
        };

        Tools =
        [
            new FunctionToolDefinition
            {
                Name = "DateTime-Now",
                Description = "Returns the current time in the format yyyy-MM-dd HH:mm:ss",
                Method = () => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            },
            new BingGroundingToolDefinition()
        ];
    }
}
