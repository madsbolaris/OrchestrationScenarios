// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using FlowCreator.Models;
using FlowCreator.Services;

namespace FlowCreator.Workflows.FlowCreation.Steps;

public sealed class CreateTriggerStep(
    FlowDefinitionService flowDocumentService,
    WorkingFlowDefinitionService workingFlowDefinitionService,
    IOptions<AaptConnectorsSettings> settings
) : KernelProcessStep
{
    [KernelFunction("create")]
    public async Task CreateAsync(KernelProcessStepContext context, CreateTriggerInput input)
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

        var connectorName = doc.ApiName.StartsWith("shared_") ? doc.ApiName[7..] : doc.ApiName;
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

        if (operationObject.TryGetProperty("parameters", out var parametersArray) &&
            parametersArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var paramOrRef in parametersArray.EnumerateArray())
            {
                JsonElement? param = null;

                if (paramOrRef.TryGetProperty("$ref", out var refNode))
                {
                    // Resolve the referenced parameter
                    var refPath = refNode.GetString();
                    if (refPath is null)
                        continue;

                    param = ResolveRefObject(swaggerDoc, refPath);
                    if (param?.ValueKind != JsonValueKind.Object)
                        continue;
                }
                else
                {
                    param = paramOrRef;
                }

                if (!param.HasValue || !param.Value.TryGetProperty("name", out var nameNode))
                    continue;

                var name = nameNode.GetString()!;
                var description = param.Value.TryGetProperty("description", out var descNode)
                    ? descNode.GetString() ?? ""
                    : "";

                string type = "string";

                if (param.Value.TryGetProperty("type", out var typeNode))
                {
                    type = typeNode.GetString() ?? "string";
                }
                else if (param.Value.TryGetProperty("schema", out var schemaNode))
                {
                    if (schemaNode.TryGetProperty("type", out var innerTypeNode))
                    {
                        type = innerTypeNode.GetString() ?? "string";
                    }
                    else if (schemaNode.TryGetProperty("$ref", out var schemaRefNode))
                    {
                        var schemaRef = schemaRefNode.GetString();
                        type = ResolveRefType(swaggerDoc, schemaRef) ?? "string";
                    }
                }

                var required = param.Value.TryGetProperty("required", out var requiredNode) &&
                            requiredNode.ValueKind == JsonValueKind.True;

                // Save the x-ms-dynamic-values property (handling nullable JsonElement)
                JsonElement? dynamicValues = null;
                if (param.Value.TryGetProperty("x-ms-dynamic-values", out var dynamicValuesNode))
                {
                    // Deep copy the dynamicValuesNode
                    string dynamicValuesJson = dynamicValuesNode.ToString();
                    dynamicValues = JsonDocument.Parse(dynamicValuesJson).RootElement;
                }

                schema.Properties[name] = new SchemaDefinition.SchemaProperty
                {
                    Type = type,
                    Description = description,
                    Required = required,
                    DynamicValues = dynamicValues // Add dynamic values here
                };
            }
        }

        doc.InputSchema = schema;

        workingFlowDefinitionService.UpdateCurrentFlowDefinition(d =>
        {
            d.InputSchema = schema;
            return d;
        });

        if (doc.ApiName is not null && doc.OperationId is not null)
        {
            if (flowDocumentService.TryUpsertFlowDefinition(doc.ApiName, doc.OperationId, d =>
            {
                d.InputSchema = schema;
                return d;
            }, doc))
            {
                await context.EmitEventAsync(SpecWorkflowEvents.EmitHelp,
                    $"The flow definition for {doc.ApiName}-{doc.OperationId} has been saved with the updated input schema.");
            }
        }

        await context.EmitEventAsync(SpecWorkflowEvents.CreateAction, new CreateActionInput());
    }

    private static string? ResolveRefType(JsonDocument doc, string? refPath)
    {
        if (string.IsNullOrWhiteSpace(refPath) || !refPath.StartsWith("#/"))
            return null;

        var tokens = refPath.Substring(2).Split('/');
        JsonElement current = doc.RootElement;

        foreach (var token in tokens)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(token, out current))
                return null;
        }

        if (current.TryGetProperty("type", out var typeNode))
            return typeNode.GetString();

        return null;
    }

    private static JsonElement? ResolveRefObject(JsonDocument doc, string? refPath)
    {
        if (string.IsNullOrWhiteSpace(refPath) || !refPath.StartsWith("#/"))
            return null;

        var tokens = refPath.Substring(2).Split('/');
        JsonElement current = doc.RootElement;

        foreach (var token in tokens)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(token, out current))
                return null;
        }

        return current;
    }
}

public class CreateTriggerInput
{
}
