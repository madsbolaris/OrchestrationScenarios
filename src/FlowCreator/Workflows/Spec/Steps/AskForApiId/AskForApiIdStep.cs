// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using FlowCreator.Services;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlowCreator.Workflows.Spec.Steps.AskForApiId;

public sealed class AskForApiIdStep(AIDocumentService documentService) : KernelProcessStep
{
    [KernelFunction("ask")]
    public async Task Ask(KernelProcessStepContext context, AskForApiIdInput input)
    {
        // Define the path to the JSON file
        var filePath = "/Users/mabolan/Repos/AAPT-connectors/src/source/tools/DocGenerator/SampleRequestResponses/PowerApps.json";

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Could not find the PowerApps.json file.", filePath);
        }

        // Read and parse the JSON
        var json = File.ReadAllText(filePath);
        using var document = JsonDocument.Parse(json);

        var apiId = input.ApiId;
        string? fullPath = null;

        foreach (var element in document.RootElement.EnumerateArray())
        {
            if (element.TryGetProperty("name", out var nameProp) && nameProp.GetString() == apiId)
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
            await context.EmitEventAsync(SpecWorkflowEvents.EmitError, $"API with name '{apiId}' not found.");
        }

        // Update the document with the full API path
        documentService.TryUpdateAIDocument(input.DocumentId, doc =>
        {
            doc.ApiId = fullPath;
            return doc;
        });
    }
}
