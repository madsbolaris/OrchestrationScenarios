// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using FlowCreator.Services;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using FlowCreator.Models;
using System.Text.Json.Nodes;

namespace FlowCreator.Workflows.Spec.Steps.AskForApiName;

public sealed class AskForApiNameStep(AIDocumentService documentService, IOptions<AaptConnectorsSettings> settings) : KernelProcessStep
{
    [KernelFunction("ask")]
    public async Task Ask(KernelProcessStepContext context, AskForApiNameInput input)
    {
        // Define the path to the JSON file
        var filePath = Path.Combine(settings.Value.FolderPath, "src/source/tools/DocGenerator/SampleRequestResponses/PowerApps.json");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Could not find the PowerApps.json file.", filePath);
        }

        // Read and parse the JSON
        var json = File.ReadAllText(filePath);
        using var document = JsonDocument.Parse(json);

        var apiName = input.ApiName;
        string? fullPath = null;

        foreach (var element in document.RootElement.EnumerateArray())
        {
            if (element.TryGetProperty("name", out var nameProp) && nameProp.GetString() == apiName)
            {
                if (element.TryGetProperty("id", out var idProp))
                {
                    fullPath = idProp.GetString();
                    break;
                }
            }
        }

        if (fullPath is null)
        {
            await context.EmitEventAsync(SpecWorkflowEvents.EmitError, $"API with name '{apiName}' not found.");
            return;
        }

         // Remove "shared_" prefix if it exists
        var connectorName = apiName.StartsWith("shared_")
            ? apiName[7..]
            : apiName;

        // Define the path to the JSON file
        var swaggerFilePath = Path.Combine(settings.Value.FolderPath, $"src/Connectors/FirstParty/{connectorName}/Connector/apidefinition.swagger.json");

        if (!File.Exists(swaggerFilePath))
        {
            throw new FileNotFoundException("Could not find the API definition file.", swaggerFilePath);
        }

        // Read and parse the Swagger JSON
        var swaggerJson = File.ReadAllText(swaggerFilePath);
        var swaggerNode = JsonNode.Parse(swaggerJson);
        var paths = swaggerNode?["paths"]?.AsObject();

        if (paths != null)
        {
            var productionOps = new List<string>();

            foreach (var path in paths)
            {
                if (path.Value is not JsonObject methods)
                    continue;

                foreach (var method in methods)
                {
                    var operation = method.Value;
                    var annotation = operation?["x-ms-api-annotation"];
                    var status = annotation?["status"]?.ToString();

                    if (status?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var opId = operation?["operationId"]?.ToString();
                        if (!string.IsNullOrEmpty(opId))
                            productionOps.Add(opId);
                    }
                }
            }

            if (productionOps.Count > 0)
            {
                await context.EmitEventAsync(SpecWorkflowEvents.EmitHelp, $"### Available operation ids:\n- {string.Join("\n- ", productionOps)}");
            }
            else
            {
                await context.EmitEventAsync(SpecWorkflowEvents.EmitHelp, "No production operations found in the Swagger file; confirm with the user if they want to proceed with the API ID provided.");
            }
        }


        // Update the document with the full API path
        documentService.TryUpdateAIDocument(input.DocumentId, doc =>
        {
            doc.ApiId = fullPath;
            doc.ApiName = apiName;
            return doc;
        });
    }
}
