using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using FlowCreator.Models;
using FlowCreator.Services;
using FlowCreator.Workflows.FlowCreation.Steps.LoadExistingFlow;

namespace FlowCreator.Workflows.FlowCreation.Steps;

public sealed class AskForApiNameStep(
    FlowDefinitionService flowDocumentService,
    WorkingFlowDefinitionService workingFlowDefinitionService,
    IOptions<AaptConnectorsSettings> settings
) : KernelProcessStep
{
    [KernelFunction("ask")]
    public async Task AskAsync(KernelProcessStepContext context, AskForApiNameInput input)
    {
        var doc = workingFlowDefinitionService.GetCurrentFlowDefinition();
        var apiName = input.ApiName;
        var connectorName = input.ConnectorName;
        var folderPath = settings.Value.FolderPath;
        var sampleFilePath = Path.Combine(folderPath, "src/source/tools/DocGenerator/SampleRequestResponses/PowerApps.json");

        if (!File.Exists(sampleFilePath))
            throw new FileNotFoundException("Could not find the PowerApps.json file.", sampleFilePath);

        var json = File.ReadAllText(sampleFilePath);
        using var document = JsonDocument.Parse(json);

        var element = document.RootElement.EnumerateArray()
            .FirstOrDefault(e => e.TryGetProperty("name", out var name) && name.GetString()!.ToLower() == apiName.ToLower());

        string? fullPath = element.ValueKind != JsonValueKind.Undefined
            ? element.GetProperty("id").GetString()
            : null;


        if (fullPath is null)
        {
            await context.EmitEventAsync(SpecWorkflowEvents.EmitError, $"API with name '{apiName}' not found.");
            return;
        }

        var swaggerPath = Path.Combine(folderPath, $"src/Connectors/FirstParty/{connectorName}/Connector/apidefinition.swagger.json");

        if (!File.Exists(swaggerPath))
        {
            await context.EmitEventAsync(SpecWorkflowEvents.EmitError, $"API with name '{apiName}' was successfully found, but a connector with name '{connectorName}' does not exist. Please ask the user to provide a valid connector name for the api `{apiName}`.");
            return;
        }

        var swaggerJson = File.ReadAllText(swaggerPath);
        var swaggerNode = JsonNode.Parse(swaggerJson);
        var paths = swaggerNode?["paths"]?.AsObject();

        if (paths is not null)
        {
            var productionOps = paths
                .SelectMany(p => p.Value is JsonObject methods
                    ? methods
                        .Where(m =>
                            m.Value is JsonObject method &&
                            // Not deprecated
                            (!method.TryGetPropertyValue("deprecated", out var deprecatedNode) || deprecatedNode?.GetValue<bool>() != true) &&
                            // Not marked internal
                            (!method.TryGetPropertyValue("x-ms-visibility", out var visibilityNode) || !string.Equals(visibilityNode?.ToString(), "internal", StringComparison.OrdinalIgnoreCase)) &&
                            // Has operationId
                            !string.IsNullOrEmpty(method["operationId"]?.ToString())
                        )
                        .Select(m => m.Value?["operationId"]?.ToString())
                    : Enumerable.Empty<string?>())
                .ToList();

            if (productionOps.Count > 0)
            {
                var helpText = $"### Available operation ids:\n- {string.Join("\n- ", productionOps)}";
                await context.EmitEventAsync(SpecWorkflowEvents.EmitHelp, helpText);
            }
            else
            {
                await context.EmitEventAsync(SpecWorkflowEvents.EmitHelp, 
                    "No non-deprecated, externally-visible operations found in the Swagger file; confirm with the user if they want to proceed with the API ID provided.");
            }
        }

        doc.ApiName = apiName;
        doc.ApiId = fullPath;
        doc.ConnectorName = connectorName;

        workingFlowDefinitionService.UpdateCurrentFlowDefinition((d) =>
        {
            d.ApiName = apiName;
            d.ApiId = fullPath;
            d.ConnectorName = connectorName;
            return d;
        });

        if (doc.ApiId is not null && doc.ApiName is not null && doc.OperationId is not null)
        {
            await context.EmitEventAsync(SpecWorkflowEvents.LoadExistingFlow, new LoadExistingFlowInput());
        }
    }
}

public class AskForApiNameInput
{
    [JsonPropertyName("apiName")]
    public required string ApiName { get; set; }

    [JsonPropertyName("connectorName")]
    public required string ConnectorName { get; set; }
}
