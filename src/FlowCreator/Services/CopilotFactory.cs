

using AgentsSdk.Models.Agents;
using AgentsSdk.Models.Agents.Models.OpenAI;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Tools.ToolDefinitions.Function;
using FlowCreator.Models;
using FlowCreator.Workflows.FlowCreation;
using Microsoft.Extensions.Options;

namespace FlowCreator.Services;

public class CopilotFactory(IServiceProvider serviceProvider)
{
    public Agent CreateCopilot()
    {
        var specWorkflow = new SpecWorkflow(serviceProvider);

        return new()
        {
            DisplayName = "Copilot",
            Description = "This agent helps the user create a new flow",
            Model = new OpenAIAgentModel { Id = "gpt-4.1" },
            Instructions =
            [
                new SystemMessage
                {
                    Content = [
                        new TextContent
                        {
                            Text = """
                            You will help a user create a new Power Automate flow that wraps a single action. To create this flow, you need to:
                            1. Set the API ID
                            2. Set the operation ID
                            """
                        }
                    ]
                }
            ],
            Tools = [
                FunctionToolDefinition.CreateToolDefinitionFromObjectMethod(specWorkflow, nameof(SpecWorkflow.UpdateApiNameAsync)),
                FunctionToolDefinition.CreateToolDefinitionFromObjectMethod(specWorkflow, nameof(SpecWorkflow.UpdateOperationIdAsync)),
                FunctionToolDefinition.CreateToolDefinitionFromObjectMethod(specWorkflow, nameof(SpecWorkflow.UpdateConnectionReferenceLogicalNameAsync))
            ]
        };
    }
}