// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using FlowCreator.Services;
using System.Text.Json;
using Microsoft.Extensions.Options;
using FlowCreator.Models;
using FlowCreator.Workflows.Spec.Steps.CreateTrigger;
using FlowCreator.Workflows.Spec.Steps.SaveFlow;

namespace FlowCreator.Workflows.Spec.Steps.LoadExistingFlow;

public sealed class LoadExistingFlowStep(AIDocumentService documentService) : KernelProcessStep
{
    [KernelFunction("load")]
    public async Task LoadAsync(KernelProcessStepContext context, LoadExistingFlowInput input)
    {
        var document = documentService.GetAIDocument(input.DocumentId);

        var fileName = $"{document.ApiName}.{document.OperationId}.json";
        var flowPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Resources", "Flows", fileName);

        if (!File.Exists(flowPath))
            return; // Nothing to load

        var json = await File.ReadAllTextAsync(flowPath);
        var loaded = JsonSerializer.Deserialize<AIDocument>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (loaded == null)
        {
            // emit create trigger event
            await context.EmitEventAsync(SpecWorkflowEvents.CreateTrigger, new CreateTriggerInput
            {
                DocumentId = document.Id
            });
            return;
        }

        // Emit that the flow was loaded
        await context.EmitEventAsync(SpecWorkflowEvents.EmitHelp, $"Flow for {document.ApiName} with operation {document.OperationId} already exists and loaded successfully.");

        if (loaded.ConnectionReferenceLogicalName == null)
        {
            // If the loaded document doesn't have a connection reference, emit an event to ask for it
            await context.EmitEventAsync(SpecWorkflowEvents.EmitHelp, $"The flow for {document.ApiName} with operation {document.OperationId} does not have a connection reference. Please provide it.");

        }

        documentService.TryUpdateAIDocument(document.Id, _ =>
        {
            // Merge in values from loaded doc
            document.InputSchema = loaded.InputSchema;
            document.ActionSchema = loaded.ActionSchema;
            document.ConnectionReferenceLogicalName = loaded.ConnectionReferenceLogicalName;
            document.Version = loaded.Version;
            return document;
        });
    }
}
