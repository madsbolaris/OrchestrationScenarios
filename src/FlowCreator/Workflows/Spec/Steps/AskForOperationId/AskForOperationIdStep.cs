// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using FlowCreator.Services;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using FlowCreator.Models;

namespace FlowCreator.Workflows.Spec.Steps.AskForOperationId;

public sealed class AskForOperationIdStep(AIDocumentService documentService, IOptions<AaptConnectorsSettings> settings) : KernelProcessStep
{
    [KernelFunction("ask")]
    public async Task Ask(KernelProcessStepContext context, AskForOperationIdInput input)
    {
        // Load the document to get the API ID
        var document = documentService.GetAIDocument(input.DocumentId);
        if (document == null || string.IsNullOrEmpty(document.ApiId))
        {
            await context.EmitEventAsync(SpecWorkflowEvents.EmitError, "No valid API ID found on the document.");
            return;
        }

        // Remove "shared_" prefix to locate Swagger file
        var connectorName = document.ApiId.Split('/').Last().Replace("shared_", string.Empty);
        var swaggerFilePath = Path.Combine(settings.Value.FolderPath, $"src/Connectors/FirstParty/{connectorName}/Connector/apidefinition.swagger.json");

        if (!File.Exists(swaggerFilePath))
        {
            throw new FileNotFoundException("Could not find the Swagger definition file.", swaggerFilePath);
        }

        var swaggerJson = File.ReadAllText(swaggerFilePath);
        var swaggerNode = JsonNode.Parse(swaggerJson);
        var paths = swaggerNode?["paths"]?.AsObject();

        var found = false;
        var productionOps = new List<string>();

        if (paths != null)
        {
            foreach (var path in paths)
            {
                if (path.Value is not JsonObject methods) continue;

                foreach (var method in methods)
                {
                    var operation = method.Value;
                    var opId = operation?["operationId"]?.ToString();
                    var status = operation?["x-ms-api-annotation"]?["status"]?.ToString();

                    if (!string.IsNullOrEmpty(opId))
                    {
                        if (status?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            productionOps.Add(opId);
                        }

                        if (opId == input.OperationId)
                        {
                            found = true;
                        }
                    }
                }
            }
        }

        if (!found)
        {
            await context.EmitEventAsync(SpecWorkflowEvents.EmitError, $"Operation ID '{input.OperationId}' not found in the Swagger file.");
            if (productionOps.Count > 0)
            {
                await context.EmitEventAsync(SpecWorkflowEvents.EmitHelp, $"### Available production operation IDs:\n- {string.Join("\n- ", productionOps)}");
            }
            return;
        }

        // Update document
        documentService.TryUpdateAIDocument(input.DocumentId, doc =>
        {
            doc.OperationId = input.OperationId;
            return doc;
        });
    }
}
