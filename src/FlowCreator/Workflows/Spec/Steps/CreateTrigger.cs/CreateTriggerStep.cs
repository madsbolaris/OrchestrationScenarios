// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using FlowCreator.Services;
using System.Text.Json;
using Microsoft.Extensions.Options;
using FlowCreator.Models;
using FlowCreator.Workflows.Spec.Steps.CreateAction;

namespace FlowCreator.Workflows.Spec.Steps.CreateTrigger;

public sealed class CreateTriggerStep(AIDocumentService documentService, IOptions<AaptConnectorsSettings> settings) : KernelProcessStep
{
    [KernelFunction("create")]
    public async Task CreateAsync(KernelProcessStepContext context, CreateTriggerInput input)
    {
        var document = documentService.GetAIDocument(input.DocumentId)!;

        var apiName = document.ApiName;
        var operationId = document.OperationId;

        if (string.IsNullOrWhiteSpace(apiName))
        {
            await context.EmitEventAsync(SpecWorkflowEvents.EmitError, "API name is missing from the document; please ask for it first before creating the trigger.");
            return;
        }

        if (string.IsNullOrWhiteSpace(operationId))
        {
            await context.EmitEventAsync(SpecWorkflowEvents.EmitError, "Operation ID is missing from the document; please ask for it first before creating the trigger.");
            return;
        }

        // Build path to the swagger file
        var connectorName = apiName.StartsWith("shared_") ? apiName[7..] : apiName;
        var swaggerFilePath = Path.Combine(
            settings.Value.FolderPath,
            $"src/Connectors/FirstParty/{connectorName}/Connector/apidefinition.swagger.json"
        );

        if (!File.Exists(swaggerFilePath))
            throw new FileNotFoundException("Swagger file not found for connector.", swaggerFilePath);

        using var doc = JsonDocument.Parse(File.ReadAllText(swaggerFilePath));
        var root = doc.RootElement;

        string? foundPath = null;
        string? foundMethod = null;
        JsonElement operationObject = default;

        foreach (var path in root.GetProperty("paths").EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject())
            {
                if (method.Value.TryGetProperty("operationId", out var opIdProp) &&
                    opIdProp.GetString() == operationId)
                {
                    foundPath = path.Name;
                    foundMethod = method.Name;
                    operationObject = method.Value;
                    break;
                }
            }

            if (foundPath != null)
                break;
        }

        if (foundPath == null)
            throw new InvalidOperationException($"Operation ID '{operationId}' not found in Swagger file.");

        var schema = new SchemaDefinition { Properties = [] };

        if (operationObject.TryGetProperty("parameters", out var parametersArray) &&
            parametersArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var param in parametersArray.EnumerateArray())
            {
                if (!param.TryGetProperty("name", out var nameNode))
                    continue; // skip unnamed parameters

                var name = nameNode.GetString() ?? "unnamed";

                string type = "string";
                if (param.TryGetProperty("schema", out var schemaNode) &&
                    schemaNode.TryGetProperty("type", out var typeNode))
                {
                    type = typeNode.GetString() ?? "string";
                }

                string description = "";
                if (param.TryGetProperty("description", out var descNode))
                {
                    description = descNode.GetString() ?? "";
                }

                schema.Properties[name] = new SchemaDefinition.SchemaProperty
                {
                    Type = type,
                    Description = description
                };
            }
        }


        documentService.TryUpdateAIDocument(document.Id, doc =>
        {
            doc.InputSchema = schema;
            return doc;
        });

        
        await context.EmitEventAsync(SpecWorkflowEvents.SaveFlow, new SaveFlowInput
        {
            DocumentId = input.DocumentId
        });

        await context.EmitEventAsync(SpecWorkflowEvents.CreateAction, new CreateActionInput
        {
            DocumentId = input.DocumentId
        });
    }
}
