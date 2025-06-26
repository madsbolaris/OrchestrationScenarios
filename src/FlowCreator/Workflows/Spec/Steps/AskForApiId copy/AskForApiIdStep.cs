// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using FlowCreator.Services;

namespace FlowCreator.Workflows.Spec.Steps.AskForOperationId;

/// <summary>
/// Converts the HTML body of an Outlook message to Markdown, referencing CID images.
/// </summary>
public sealed class AskForOperationIdStep(AIDocumentService documentService) : KernelProcessStep
{
    [KernelFunction("ask")]
    public void Ask(KernelProcessStepContext context, AskForOperationIdInput input)
    {
        documentService.TryUpdateAIDocument(input.DocumentId, doc =>
        {
            doc.OperationId = input.OperationId;
            return doc;
        });


    }
}
