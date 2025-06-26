using System.Text.Json;
using Microsoft.SemanticKernel;
using FlowCreator.Models;
using FlowCreator.Services;

namespace FlowCreator.Workflows.FlowCreation.Steps.LoadExistingFlow;

public sealed class LoadExistingFlowStep(
    FlowDefinitionService flowDocumentService,
    WorkingFlowDefinitionService workingFlowDefinitionService
) : KernelProcessStep
{
    [KernelFunction("load")]
    public async Task LoadAsync(KernelProcessStepContext context, LoadExistingFlowInput input)
    {
        var doc = workingFlowDefinitionService.GetCurrentFlowDefinition();

        var loaded = flowDocumentService.GetFlowDefinition(doc.ApiName!, doc.OperationId!);

        if (loaded == null)
        {
            await context.EmitEventAsync(SpecWorkflowEvents.CreateTrigger, new CreateTriggerInput());
            return;
        }

        workingFlowDefinitionService.UpdateCurrentFlowDefinition(d =>
        {
            d.ApiId = loaded.ApiId;
            d.ConnectionReferenceLogicalName = loaded.ConnectionReferenceLogicalName;
            d.InputSchema = loaded.InputSchema;
            d.ActionSchema = loaded.ActionSchema;
            return d;
        });

        await context.EmitEventAsync(SpecWorkflowEvents.EmitHelp,
            $"Flow for {doc.ApiName} with operation {doc.OperationId} already exists and loaded successfully.");

        if (loaded?.ConnectionReferenceLogicalName is null)
        {
            await context.EmitEventAsync(SpecWorkflowEvents.EmitHelp,
                $"The flow for {doc.ApiName} with operation {doc.OperationId} does not have a connection reference. Please provide it.");
        }
    }
}

public class LoadExistingFlowInput
{
}
