using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using FlowCreator.Models;
using FlowCreator.Services;
using FlowCreator.Workflows.FlowCreation.Steps.LoadExistingFlow;

namespace FlowCreator.Workflows.FlowCreation.Steps;

public sealed class AskForOperationIdStep(
    FlowDefinitionService flowDocumentService,
    WorkingFlowDefinitionService workingFlowDefinitionService,
    IOptions<AaptConnectorsSettings> settings
) : KernelProcessStep
{
    [KernelFunction("ask")]
    public async Task AskAsync(KernelProcessStepContext context, AskForOperationIdInput input)
    {
        var doc = workingFlowDefinitionService.GetCurrentFlowDefinition();

        if (doc.ApiName is null)
        {
            await context.EmitEventAsync(SpecWorkflowEvents.EmitError, "No valid API name found on the document yet.");
            return;
        }

        var connectorName = doc.ApiName.Replace("shared_", string.Empty);
        var swaggerPath = Path.Combine(settings.Value.FolderPath, $"src/Connectors/FirstParty/{connectorName}/Connector/apidefinition.swagger.json");

        if (!File.Exists(swaggerPath))
            throw new FileNotFoundException("Could not find the Swagger definition file.", swaggerPath);

        var swaggerJson = File.ReadAllText(swaggerPath);
        var swaggerNode = JsonNode.Parse(swaggerJson);
        var paths = swaggerNode?["paths"]?.AsObject();

        var productionOps = new List<string>();
        var found = false;

        if (paths is not null)
        {
            foreach (var (_, methods) in paths)
            {
                if (methods is not JsonObject methodSet)
                    continue;

                foreach (var (_, operation) in methodSet)
                {
                    var opId = operation?["operationId"]?.ToString();
                    var status = operation?["x-ms-api-annotation"]?["status"]?.ToString();

                    if (!string.IsNullOrEmpty(opId))
                    {
                        if (status?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true)
                            productionOps.Add(opId);

                        if (opId == input.OperationId)
                            found = true;
                    }
                }
            }
        }

        if (!found)
        {
            await context.EmitEventAsync(SpecWorkflowEvents.EmitError, $"Operation ID '{input.OperationId}' not found in the Swagger file.");

            if (productionOps.Count > 0)
            {
                var helpText = $"### Available production operation IDs:\n- {string.Join("\n- ", productionOps)}";
                await context.EmitEventAsync(SpecWorkflowEvents.EmitHelp, helpText);
            }

            return;
        }

        doc.OperationId = input.OperationId;

        workingFlowDefinitionService.UpdateCurrentFlowDefinition(d =>
        {
            d.OperationId = input.OperationId;
            return d;
        });

        if (doc.ApiId is not null && doc.ApiName is not null && doc.OperationId is not null)
        {
            await context.EmitEventAsync(SpecWorkflowEvents.LoadExistingFlow, new LoadExistingFlowInput());
        }
    }
}

public class AskForOperationIdInput
{
    [JsonPropertyName("operationId")]
    public required string OperationId { get; set; }
}
