

using AgentsSdk.Models.Agents.Models.OpenAI;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Tools.ToolDefinitions.Function;
using FlowCreator.Agents;
using FlowCreator.Workflows.Spec;

namespace FlowCreator.Services;

public class CopilotFactory(AIDocumentService aIDocumentService)
{

    public Copilot CreateCopilot(Guid id)
    {
        var specWorkflow = new SpecWorkflow(aIDocumentService, id);

        return new(specWorkflow)
        {
            DisplayName = "Copilot",
            Description = "This agent help the user create a new flow",
            Model = new OpenAIAgentModel
            {
                Id = "gpt-4.1"
            },
            Instructions =
            [
                new SystemMessage
                {
                    Content =  [new TextContent() { Text = """
                    You will help a user create a new Power Automate flow that wraps a single action. To create this flow, you need to:
                    1. Set the API ID
                    """ }]
                }
            ],
            Tools = [
                new FunctionToolDefinition
                {
                    Name = "AskForApiId",
                    Description = "Ask the user for the ID of the API they want to use.",
                    Method = specWorkflow.UpdateApiIdAsync
                }
            ]
        };
    }
}