// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using FlowCreator.Services;
using System.Text.Json;
using Microsoft.Extensions.Options;
using FlowCreator.Models;
using FlowCreator.Workflows.Spec.Steps.SaveFlow;

namespace FlowCreator.Workflows.Spec.Steps.CreateAction;

public sealed class CreateActionStep(AIDocumentService documentService) : KernelProcessStep
{
    [KernelFunction("create")]
    public async Task CreateAsync(KernelProcessStepContext context, CreateActionInput input)
    {
        var doc = documentService.GetAIDocument(input.DocumentId);

        if (string.IsNullOrWhiteSpace(doc.ApiName) || string.IsNullOrWhiteSpace(doc.ApiId) || string.IsNullOrWhiteSpace(doc.OperationId))
            throw new InvalidOperationException("Document must contain ApiName, ApiId, and OperationId before creating an action.");

        // Build parameter map with triggerBody expressions
        var parameters = new Dictionary<string, object>();

        if (doc.InputSchema?.Properties is not null)
        {
            foreach (var kvp in doc.InputSchema.Properties)
            {
                var paramName = kvp.Key;
                parameters[paramName] = $"@triggerBody()?['{paramName}']";
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

        documentService.TryUpdateAIDocument(doc.Id, d =>
        {
            d.ActionSchema = action;
            return d;
        });

        await context.EmitEventAsync(SpecWorkflowEvents.SaveFlow, new SaveFlowInput
        {
            DocumentId = doc.Id
        });
    }
}
