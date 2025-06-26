using Microsoft.SemanticKernel;
using FlowCreator.Models;
using FlowCreator.Services;

namespace FlowCreator.Workflows.FlowCreation.Steps;

public sealed class CreateActionStep(
    FlowDefinitionService flowDocumentService,
    WorkingFlowDefinitionService workingFlowDefinitionService
) : KernelProcessStep
{
    [KernelFunction("create")]
    public async Task CreateAsync(KernelProcessStepContext context, CreateActionInput input)
    {
        var doc = workingFlowDefinitionService.GetCurrentFlowDefinition();

        var parameters = new Dictionary<string, object>();

        if (doc.InputSchema?.Properties is not null)
        {
            foreach (var (key, _) in doc.InputSchema.Properties)
            {
                parameters[key] = $"@triggerBody()?['{key}']";
            }
        }

        var action = new FlowAction
        {
            Type = "OpenApiConnection",
            Inputs = new FlowAction.FlowActionInputs
            {
                Host = new FlowAction.FlowActionInputs.FlowHost
                {
                    ConnectionName = doc.ApiName,
                    ApiId = doc.ApiId,
                    OperationId = doc.OperationId
                },
                Parameters = parameters,
                Authentication = null
            }
        };

        doc.ActionSchema = action;

        workingFlowDefinitionService.UpdateCurrentFlowDefinition((d) =>
        {
            d.ActionSchema = action;
            return d;
        });

        if (doc.ApiName is not null && doc.OperationId is not null)
        {
            if (flowDocumentService.TryUpsertFlowDefinition(doc.ApiName, doc.OperationId, d =>
            {
                d.ActionSchema = action;
                return d;
            }, doc))
            {
                await context.EmitEventAsync(SpecWorkflowEvents.EmitHelp,
                    $"The flow definition for {doc.ApiName}.{doc.OperationId} has been saved with the updated action schema'.");
            }
        }
    }
}

public class CreateActionInput
{
}
