using System.Reflection.Metadata;
using FlowCreator.Models;
using FlowCreator.Services;
using FlowCreator.Workflows.Spec.Summary.Steps;
using Mauve.Workflows.GenerateEmailMarkdown;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace FlowCreator.Workflows.Spec;

public class SpecWorkflow
{
    private readonly KernelProcess _process;
    private readonly Guid _documentId;
    private readonly AIDocumentService _documentService;

    public SpecWorkflow(AIDocumentService documentService, Guid documentId)
    {
        _documentId = documentId;
        _documentService = documentService;
        var process = new ProcessBuilder("GenerateEmailMarkdown");

        var askForIdStep = process.AddStepFromType<AskForApiIdStep>();

        process
            .OnInputEvent(SpecWorkflowEvents.AskForApiId)
            .SendEventTo(new ProcessFunctionTargetBuilder(askForIdStep, "ask"));

        _process = process.Build();
    }
    
    public async Task<AIDocument> UpdateApiIdAsync(string apiId)
    {
        AIDocument document = _documentService.GetAIDocument(_documentId)!;

        await _process.StartAsync(new Kernel(), new KernelProcessEvent
        {
            Id = SpecWorkflowEvents.AskForApiId,
            Data = new AskForApiIdInput
            {
                ApiId = apiId,
                Document = document
            },
        });

        return _documentService.GetAIDocument(_documentId)!;
    }
}
