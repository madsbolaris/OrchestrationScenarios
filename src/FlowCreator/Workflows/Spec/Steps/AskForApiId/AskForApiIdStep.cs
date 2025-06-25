// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using FlowCreator.Services;

namespace FlowCreator.Workflows.Spec.Summary.Steps;

/// <summary>
/// Converts the HTML body of an Outlook message to Markdown, referencing CID images.
/// </summary>
public sealed class AskForApiIdStep(AIDocumentService documentService) : KernelProcessStep
{
    [KernelFunction("ask")]
    public void Ask(KernelProcessStepContext context, AskForApiIdInput input)
    {
        documentService.TryUpdateAIDocument(input.Document.Id, doc =>
        {
            doc.ApiId = input.ApiId;
            return doc;
        });
    }
}
