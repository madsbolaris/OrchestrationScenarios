using System.ComponentModel;
using FlowCreator.Models;
using FlowCreator.Services;
using FlowCreator.Workflows.Spec.Steps.AskForOperationId;
using FlowCreator.Workflows.Spec.Steps.AskForApiId;
using FlowCreator.Workflows.Spec;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace FlowCreator.Workflows.Spec;

public class SpecWorkflow
{
    private readonly KernelProcess _process;
    private readonly Guid _documentId;
    private readonly AIDocumentService _documentService;
    private readonly Kernel _kernel;
    private readonly JsonSerializerOptions? _jsonSerializerOptions;

    public SpecWorkflow(AIDocumentService documentService, Guid documentId, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _documentId = documentId;
        _documentService = documentService;
        _jsonSerializerOptions = jsonSerializerOptions;

        var serviceProvider = new ServiceCollection()
            .AddSingleton(documentService)
            .BuildServiceProvider();

        _kernel = new Kernel(serviceProvider);
        var processBuilder = new ProcessBuilder("GenerateFlow");

        var askForIdStep = processBuilder.AddStepFromType<AskForApiIdStep>();
        var errorHandler = processBuilder.AddProxyStep(id: "errorHandler", [SpecWorkflowExternalTopics.RelayError]);

        processBuilder
            .OnInputEvent(SpecWorkflowEvents.AskForApiId)
            .SendEventTo(new ProcessFunctionTargetBuilder(askForIdStep, "ask"));

        askForIdStep
            .OnEvent(SpecWorkflowEvents.EmitError)
            .EmitExternalEvent(errorHandler, SpecWorkflowExternalTopics.RelayError);

        _process = processBuilder.Build();
    }

    public async Task<string> UpdateApiIdAsync(
        [Description("The name of the API provided by the user; should begin with `shared_`. If given a full path, just provide the last part after the last `/`; the function will then resolve it to the correct API.")] string apiId
    )
    {
        var externalMessageChannel = new ErrorHandlerChannel();
        var context = await _process.StartAsync(_kernel, new KernelProcessEvent
        {
            Id = SpecWorkflowEvents.AskForApiId,
            Data = new AskForApiIdInput
            {
                ApiId = apiId,
                DocumentId = _documentId
            },
        }, externalMessageChannel);

        if (externalMessageChannel.GetErrors().Count > 0)
        {
            return string.Join("\n", externalMessageChannel.GetErrors());
        }

        return JsonSerializer.Serialize(
            _documentService.GetAIDocument(_documentId),
            _jsonSerializerOptions
        );
    }

    public async Task<AIDocument> UpdateOperationIdAsync(string operationId)
    {
        await _process.StartAsync(_kernel, new KernelProcessEvent
        {
            Id = SpecWorkflowEvents.AskForOperationId,
            Data = new AskForOperationIdInput
            {
                OperationId = operationId,
                DocumentId = _documentId
            },
        });

        return _documentService.GetAIDocument(_documentId)!;
    }
}


public class ErrorHandlerChannel : IExternalKernelProcessMessageChannel
{
    private List<string> _errors = [];

    public Task EmitExternalEventAsync(string externalTopicEvent, KernelProcessProxyMessage message)
    {
        if (externalTopicEvent == SpecWorkflowExternalTopics.RelayError)
        {
            if (message.EventData!.Content is string errorMessage)
            {
                _errors.Add(JsonSerializer.Deserialize<string>(errorMessage)!);
            }
        }
        return Task.CompletedTask;
    }

    public IReadOnlyList<string> GetErrors() => _errors.AsReadOnly();

    public ValueTask Initialize()
    {
        _errors = new List<string>();
        return ValueTask.CompletedTask;
    }

    public ValueTask Uninitialize()
    {
        _errors.Clear();
        return ValueTask.CompletedTask;
    }
}