// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using FlowCreator.Models;
using FlowCreator.Services;

namespace FlowCreator.Workflows.FlowCreation.Steps;

public sealed class CreateSummaryStep(
    FlowDefinitionService flowDocumentService,
    WorkingFlowDefinitionService workingFlowDefinitionService,
    IOptions<AaptConnectorsSettings> settings
) : KernelProcessStep
{
    [KernelFunction("create")]
    public async Task CreateAsync(KernelProcessStepContext context, CreateSummaryInput input)
    {
        var doc = workingFlowDefinitionService.GetCurrentFlowDefinition();

        if (doc.ApiName is null)
        {
            await context.EmitEventAsync(SpecWorkflowEvents.EmitError,
                "API name is missing from the document; please ask for it first before creating the trigger.");
            return;
        }

        if (doc.OperationId is null)
        {
            await context.EmitEventAsync(SpecWorkflowEvents.EmitError,
                "Operation ID is missing from the document; please ask for it first before creating the trigger.");
            return;
        }

        var connectorName = doc.ConnectorName;
        var swaggerPath = Path.Combine(
            settings.Value.FolderPath,
            $"src/Connectors/FirstParty/{connectorName}/Connector/apidefinition.swagger.json"
        );

        if (!File.Exists(swaggerPath))
            throw new FileNotFoundException("Swagger file not found for connector.", swaggerPath);

        using var swaggerDoc = JsonDocument.Parse(File.ReadAllText(swaggerPath));
        var paths = swaggerDoc.RootElement.GetProperty("paths");

        string? foundPath = null;
        string? foundMethod = null;
        JsonElement operationObject = default;

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject())
            {
                if (method.Value.TryGetProperty("operationId", out var opIdProp) &&
                    opIdProp.GetString() == doc.OperationId)
                {
                    foundPath = path.Name;
                    foundMethod = method.Name;
                    operationObject = method.Value;
                    break;
                }
            }

            if (foundPath is not null)
                break;
        }

        if (foundPath is null)
            throw new InvalidOperationException($"Operation ID '{doc.OperationId}' not found in Swagger file.");

        var schema = new SchemaDefinition { Properties = [] };

        if (operationObject.TryGetProperty("summary", out var summaryNode))
        {
            doc.Summary = summaryNode.GetString();
        }
        else
        {
            doc.Summary = string.Empty;
        }

        if (operationObject.TryGetProperty("description", out var descriptionNode))
        {
            doc.Description = descriptionNode.GetString();
        }
        else
        {
            doc.Description = string.Empty;
        }

        workingFlowDefinitionService.UpdateCurrentFlowDefinition(d =>
        {
            d.Summary = doc.Summary;
            d.Description = doc.Description;
            return d;
        });

        if (doc.ApiName is not null && doc.OperationId is not null)
        {
            if (flowDocumentService.TryUpsertFlowDefinition(doc.ApiName, doc.OperationId, d =>
            {
                d.Summary = doc.Summary;
                d.Description = doc.Description;
                return d;
            }, doc))
            {
                await context.EmitEventAsync(SpecWorkflowEvents.EmitHelp,
                    $"The flow definition for {doc.ApiName}-{doc.OperationId} has been saved with the updated summary.");
            }
        }

        await context.EmitEventAsync(SpecWorkflowEvents.CreateTrigger, new CreateTriggerInput());
    }
}

public class CreateSummaryInput
{
}
