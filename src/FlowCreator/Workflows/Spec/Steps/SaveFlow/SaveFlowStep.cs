// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using FlowCreator.Services;
using System.Text.Json;

namespace FlowCreator.Workflows.Spec.Steps.SaveFlow;

public sealed class SaveFlowStep(AIDocumentService documentService) : KernelProcessStep
{
    [KernelFunction("save")]
    public Task SaveAsync(KernelProcessStepContext context, SaveFlowInput input)
    {
        var document = documentService.GetAIDocument(input.DocumentId);

        if (document.ApiName == null || document.OperationId == null)
        {
            return Task.CompletedTask;
        }

        var fileName = $"{document.ApiName}.{document.OperationId}.json";
        var outputDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Resources", "Flows");
        Directory.CreateDirectory(outputDir);

        var filePath = Path.Combine(outputDir, fileName);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var stream = File.Create(filePath);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        JsonSerializer.Serialize(writer, document, options);

        return Task.CompletedTask;
    }
}
