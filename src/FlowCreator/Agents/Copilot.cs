

using AgentsSdk.Models.Agents;
using AgentsSdk.Models.Agents.Models;
using AgentsSdk.Models.Agents.Models.OpenAI;
using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Tools.ToolDefinitions.Function;
using FlowCreator.Services;
using FlowCreator.Workflows.Spec;

namespace FlowCreator.Agents;

public class Copilot : Agent
{

    public Copilot(SpecWorkflow specWorkflow)
    {
        DisplayName = "Copilot";
        Description = "This agent help the user create a new flow";
        Model = new OpenAIAgentModel
        {
            Id = "gpt-4.1"
        };
        Instructions =
        [
            new SystemMessage
            {
                Content =  [new TextContent() { Text = """
                You will help a user create a new Power Automate flow that wraps a single action. To create this flow, you need to:
                1. Set the API ID
                """ }]
            }
        ];
        Tools = [
            new FunctionToolDefinition
            {
                Name = "AskForApiId",
                Description = "Ask the user for the ID of the API they want to use.",
                Method = specWorkflow.UpdateApiIdAsync
            }
        ];
    }
}