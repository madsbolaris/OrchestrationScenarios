using System.ComponentModel;
using FlowCreator.Services;
using FlowCreator.Workflows.Spec.Steps.AskForOperationId;
using FlowCreator.Workflows.Spec.Steps.AskForApiName;
using FlowCreator.Workflows.Spec.Channels;
using Microsoft.SemanticKernel;
using System.Text.Json;
using FlowCreator.Workflows.Spec.Steps.AskForConnectionReferenceLogicalName;
using FlowCreator.Workflows.Spec.Steps.CreateTrigger;
using Microsoft.SemanticKernel.Process.Models;
using FlowCreator.Workflows.Spec.Steps.CreateAction;

namespace FlowCreator.Workflows.Spec;

public class SpecWorkflow
{
    private KernelProcessStateMetadata? _kernelProcessStateMetadata = null;
    private readonly KernelProcess _process;
    private readonly Guid _documentId;
    private readonly AIDocumentService _documentService;
    private readonly Kernel _kernel;
    private readonly JsonSerializerOptions? _jsonOptions;

    public SpecWorkflow(
        IServiceProvider serviceProvider,
        AIDocumentService documentService,
        Guid documentId,
        JsonSerializerOptions? jsonOptions = null)
    {
        _documentId = documentId;
        _documentService = documentService;
        _jsonOptions = jsonOptions;

        _kernel = new Kernel(serviceProvider);
        _process = BuildProcess();
    }

    private KernelProcess BuildProcess()
    {
        var builder = new ProcessBuilder("GenerateFlow");

        var askForApiName = builder.AddStepFromType<AskForApiNameStep>();
        var askForOperationId = builder.AddStepFromType<AskForOperationIdStep>();
        var askForConnectionReferenceLogicalName = builder.AddStepFromType<AskForConnectionReferenceLogicalNameStep>();
        var createTrigger = builder.AddStepFromType<CreateTriggerStep>();
        var createAction = builder.AddStepFromType<CreateActionStep>();
        var saveFlow = builder.AddStepFromType<SaveFlowStep>();

        var askForApiNameExternalHandler = builder.AddProxyStep("askForApiNameHandler", [
            SpecWorkflowExternalTopics.RelayError,
            SpecWorkflowExternalTopics.RelayHelp
        ]);

        var askForOperationIdExternalHandler = builder.AddProxyStep("askForOperationIdHandler", [
            SpecWorkflowExternalTopics.RelayError,
            SpecWorkflowExternalTopics.RelayHelp
        ]);

        var askForConnectionReferenceLogicalNameExternalHandler = builder.AddProxyStep("askForConnectionReferenceLogicalNameHandler", [
            SpecWorkflowExternalTopics.RelayError,
            SpecWorkflowExternalTopics.RelayHelp
        ]);

        var createTriggerExternalHandler = builder.AddProxyStep("createTriggerHandler", [
            SpecWorkflowExternalTopics.RelayError,
            SpecWorkflowExternalTopics.RelayHelp
        ]);

        var createActionExternalHandler = builder.AddProxyStep("createActionHandler", [
            SpecWorkflowExternalTopics.RelayError,
            SpecWorkflowExternalTopics.RelayHelp
        ]);

        builder.OnInputEvent(SpecWorkflowEvents.AskForApiName)
            .SendEventTo(new ProcessFunctionTargetBuilder(askForApiName, "ask"));

        builder.OnInputEvent(SpecWorkflowEvents.AskForOperationId)
            .SendEventTo(new ProcessFunctionTargetBuilder(askForOperationId, "ask"));

        builder.OnInputEvent(SpecWorkflowEvents.AskForConnectionReferenceLogicalName)
            .SendEventTo(new ProcessFunctionTargetBuilder(askForConnectionReferenceLogicalName, "ask"));

        askForApiName.OnEvent(SpecWorkflowEvents.CreateTrigger)
            .SendEventTo(new ProcessFunctionTargetBuilder(createTrigger, "create"));

        askForApiName.OnEvent(SpecWorkflowEvents.SaveFlow)
            .SendEventTo(new ProcessFunctionTargetBuilder(saveFlow, "save"));

        askForOperationId.OnEvent(SpecWorkflowEvents.CreateTrigger)
            .SendEventTo(new ProcessFunctionTargetBuilder(createTrigger, "create"));

        askForOperationId.OnEvent(SpecWorkflowEvents.SaveFlow)
            .SendEventTo(new ProcessFunctionTargetBuilder(saveFlow, "save"));

        createTrigger.OnEvent(SpecWorkflowEvents.CreateAction)
            .SendEventTo(new ProcessFunctionTargetBuilder(createAction, "create"));

        createTrigger.OnEvent(SpecWorkflowEvents.SaveFlow)
            .SendEventTo(new ProcessFunctionTargetBuilder(saveFlow, "save"));

        createAction.OnEvent(SpecWorkflowEvents.SaveFlow)
            .SendEventTo(new ProcessFunctionTargetBuilder(saveFlow, "save"));

        askForApiName.OnEvent(SpecWorkflowEvents.EmitError)
            .EmitExternalEvent(askForApiNameExternalHandler, SpecWorkflowExternalTopics.RelayError);

        askForApiName.OnEvent(SpecWorkflowEvents.EmitHelp)
            .EmitExternalEvent(askForApiNameExternalHandler, SpecWorkflowExternalTopics.RelayHelp);

        askForOperationId.OnEvent(SpecWorkflowEvents.EmitError)
            .EmitExternalEvent(askForOperationIdExternalHandler, SpecWorkflowExternalTopics.RelayError);

        askForOperationId.OnEvent(SpecWorkflowEvents.EmitHelp)
            .EmitExternalEvent(askForOperationIdExternalHandler, SpecWorkflowExternalTopics.RelayHelp);

        askForConnectionReferenceLogicalName.OnEvent(SpecWorkflowEvents.EmitError)
            .EmitExternalEvent(askForConnectionReferenceLogicalNameExternalHandler, SpecWorkflowExternalTopics.RelayError);

        askForConnectionReferenceLogicalName.OnEvent(SpecWorkflowEvents.EmitHelp)
            .EmitExternalEvent(askForConnectionReferenceLogicalNameExternalHandler, SpecWorkflowExternalTopics.RelayHelp);

        createTrigger.OnEvent(SpecWorkflowEvents.EmitError)
            .EmitExternalEvent(createTriggerExternalHandler, SpecWorkflowExternalTopics.RelayError);

        createTrigger.OnEvent(SpecWorkflowEvents.EmitHelp)
            .EmitExternalEvent(createTriggerExternalHandler, SpecWorkflowExternalTopics.RelayHelp);

        createAction.OnEvent(SpecWorkflowEvents.EmitError)
            .EmitExternalEvent(createActionExternalHandler, SpecWorkflowExternalTopics.RelayError);

        createAction.OnEvent(SpecWorkflowEvents.EmitHelp)
            .EmitExternalEvent(createActionExternalHandler, SpecWorkflowExternalTopics.RelayHelp);

        return builder.Build();
    }

    [KernelFunction("update_api_id")]
    [Description("Must be called whenever the user provides the API name. This function will use the API name to also get the API ID.")]
    public Task<string> UpdateApiNameAsync(
        [Description("The name of the API provided by the user; should begin with `shared_`. If given a full path, just provide the last part after the last `/`; the function will then resolve it to the correct API.")]
        string apiName)
    {
        var input = new AskForApiNameInput { ApiName = apiName, DocumentId = _documentId };
        return RunStepAsync(SpecWorkflowEvents.AskForApiName, input);
    }

    [KernelFunction("update_operation_id")]
    [Description("Must be called whenever the user provides the operation ID. You'll get a list of valid operation IDs from UpdateApiNameAsync")]
    public Task<string> UpdateOperationIdAsync(string operationId)
    {
        var input = new AskForOperationIdInput { OperationId = operationId, DocumentId = _documentId };
        return RunStepAsync(SpecWorkflowEvents.AskForOperationId, input);
    }

    [KernelFunction("update_connection_reference_logical_name")]
    [Description("Must be called whenever the user provides the connection reference logical name. This function will validate the logical name against the Dataverse environment and update the document accordingly.")]
    public Task<string> UpdateConnectionReferenceLogicalNameAsync(string logicalName)
    {
        var input = new AskForConnectionReferenceLogicalNameInput { ConnectionReferenceLogicalName = logicalName, DocumentId = _documentId };
        return RunStepAsync(SpecWorkflowEvents.AskForConnectionReferenceLogicalName, input);
    }

    private async Task<string> RunStepAsync(string eventId, object input)
    {
        var helpChannel = new HelpHandlerChannel();

        var context = await _process.StartAsync(_kernel, new KernelProcessEvent
        {
            Id = eventId,
            Data = input
        }, helpChannel);

        var output = new List<string>();

        if (helpChannel.GetHelpContent() is { Count: > 0 } help)
        {
            output.Add("## Help Content:");
            output.AddRange(help);
        }

        if (helpChannel.GetErrors() is { Count: > 0 } errors)
        {
            output.Add("## Errors:");
            output.AddRange(errors);
            return string.Join("\n", output);
        }

        var docJson = JsonSerializer.Serialize(_documentService.GetAIDocument(_documentId), _jsonOptions);
        output.Insert(0, "## Document:\n" + docJson);

        return string.Join("\n", output);
    }
}
