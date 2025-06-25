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
    private readonly Kernel _kernel;

    public SpecWorkflow(AIDocumentService documentService, Guid documentId)
    {
        _documentId = documentId;
        _documentService = documentService;

        var serviceProvider = new ServiceCollection()
            .AddSingleton(documentService)
            .BuildServiceProvider();

        _kernel = new Kernel(serviceProvider);
        var process = new ProcessBuilder("GenerateEmailMarkdown");

        var askForIdStep = process.AddStepFromType<AskForApiIdStep>();

        process
            .OnInputEvent(SpecWorkflowEvents.AskForApiId)
            .SendEventTo(new ProcessFunctionTargetBuilder(askForIdStep, "ask"));

        _process = process.Build();
    }
    
    public async Task<AIDocument> UpdateApiIdAsync(string apiId)
    {
        await _process.StartAsync(_kernel, new KernelProcessEvent
        {
            Id = SpecWorkflowEvents.AskForApiId,
            Data = new AskForApiIdInput
            {
                ApiId = apiId,
                DocumentId = _documentId
            },
        });

        return _documentService.GetAIDocument(_documentId)!;
    }
}
