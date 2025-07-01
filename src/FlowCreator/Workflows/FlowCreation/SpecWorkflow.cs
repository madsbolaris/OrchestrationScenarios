using System.ComponentModel;
using System.Text.Json;
using FlowCreator.Models;
using FlowCreator.Services;
using FlowCreator.Workflows.FlowCreation.Channels;
using FlowCreator.Workflows.FlowCreation.Steps;
using FlowCreator.Workflows.FlowCreation.Steps.AskForConnectionReferenceLogicalName;
using FlowCreator.Workflows.FlowCreation.Steps.LoadExistingFlow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace FlowCreator.Workflows.FlowCreation;

public class SpecWorkflow
{
    private readonly Kernel _kernel;
    private readonly KernelProcess _process;
    private readonly JsonSerializerOptions? _jsonOptions;
    private readonly WorkingFlowDefinitionService _workingFlowDefinitionService;

    public SpecWorkflow(
        IServiceProvider serviceProvider)
    {
        _jsonOptions = serviceProvider.GetRequiredService<JsonSerializerOptions>();
        _workingFlowDefinitionService = new WorkingFlowDefinitionService();

        ServiceCollection serviceCollection = new();
        serviceCollection.AddSingleton(serviceProvider.GetRequiredService<FlowDefinitionService>());
        serviceCollection.AddSingleton(serviceProvider.GetRequiredService<IOptions<AaptConnectorsSettings>>());
        serviceCollection.AddSingleton(serviceProvider.GetRequiredService<IOptions<DataverseSettings>>());
        serviceCollection.AddSingleton(_workingFlowDefinitionService);

        _kernel = new Kernel(serviceCollection.BuildServiceProvider());
        _process = BuildProcess();
    }

    private KernelProcess BuildProcess()
    {
        var builder = new ProcessBuilder("GenerateFlow");

        var askForApiName = builder.AddStepFromType<AskForApiNameStep>();
        var askForOperationId = builder.AddStepFromType<AskForOperationIdStep>();
        var askForConnectionReferenceLogicalName = builder.AddStepFromType<AskForConnectionReferenceLogicalNameStep>();
        var loadExistingFlow = builder.AddStepFromType<LoadExistingFlowStep>();
        var createSummary = builder.AddStepFromType<CreateSummaryStep>();
        var createTrigger = builder.AddStepFromType<CreateTriggerStep>();
        var createAction = builder.AddStepFromType<CreateActionStep>();

        var askForApiNameHandler = builder.AddProxyStep("askForApiNameHandler", [
            SpecWorkflowExternalTopics.RelayError,
            SpecWorkflowExternalTopics.RelayHelp
        ]);

        var askForOperationIdHandler = builder.AddProxyStep("askForOperationIdHandler", [
            SpecWorkflowExternalTopics.RelayError,
            SpecWorkflowExternalTopics.RelayHelp
        ]);

        var askForConnectionReferenceLogicalNameHandler = builder.AddProxyStep("askForConnectionReferenceLogicalNameHandler", [
            SpecWorkflowExternalTopics.RelayError,
            SpecWorkflowExternalTopics.RelayHelp
        ]);

        var loadFlowHandler = builder.AddProxyStep("loadFlowHandler", [
            SpecWorkflowExternalTopics.RelayError,
            SpecWorkflowExternalTopics.RelayHelp
        ]);

        var createSummaryHandler = builder.AddProxyStep("createSummaryHandler", [
            SpecWorkflowExternalTopics.RelayError,
            SpecWorkflowExternalTopics.RelayHelp
        ]);

        var createTriggerHandler = builder.AddProxyStep("createTriggerHandler", [
            SpecWorkflowExternalTopics.RelayError,
            SpecWorkflowExternalTopics.RelayHelp
        ]);

        var createActionHandler = builder.AddProxyStep("createActionHandler", [
            SpecWorkflowExternalTopics.RelayError,
            SpecWorkflowExternalTopics.RelayHelp
        ]);

        builder.OnInputEvent(SpecWorkflowEvents.AskForApiName)
            .SendEventTo(new ProcessFunctionTargetBuilder(askForApiName, "ask"));

        builder.OnInputEvent(SpecWorkflowEvents.AskForOperationId)
            .SendEventTo(new ProcessFunctionTargetBuilder(askForOperationId, "ask"));

        builder.OnInputEvent(SpecWorkflowEvents.AskForConnectionReferenceLogicalName)
            .SendEventTo(new ProcessFunctionTargetBuilder(askForConnectionReferenceLogicalName, "ask"));

        askForApiName.OnEvent(SpecWorkflowEvents.LoadExistingFlow)
            .SendEventTo(new ProcessFunctionTargetBuilder(loadExistingFlow, "load"));

        askForOperationId.OnEvent(SpecWorkflowEvents.LoadExistingFlow)
            .SendEventTo(new ProcessFunctionTargetBuilder(loadExistingFlow, "load"));

        loadExistingFlow.OnEvent(SpecWorkflowEvents.CreateSummary)
            .SendEventTo(new ProcessFunctionTargetBuilder(createSummary, "create"));

        createSummary.OnEvent(SpecWorkflowEvents.CreateTrigger)
            .SendEventTo(new ProcessFunctionTargetBuilder(createTrigger, "create"));

        createTrigger.OnEvent(SpecWorkflowEvents.CreateAction)
            .SendEventTo(new ProcessFunctionTargetBuilder(createAction, "create"));

        // Help/Error relays
        askForApiName.OnEvent(SpecWorkflowEvents.EmitError).EmitExternalEvent(askForApiNameHandler, SpecWorkflowExternalTopics.RelayError);
        askForApiName.OnEvent(SpecWorkflowEvents.EmitHelp).EmitExternalEvent(askForApiNameHandler, SpecWorkflowExternalTopics.RelayHelp);

        askForOperationId.OnEvent(SpecWorkflowEvents.EmitError).EmitExternalEvent(askForOperationIdHandler, SpecWorkflowExternalTopics.RelayError);
        askForOperationId.OnEvent(SpecWorkflowEvents.EmitHelp).EmitExternalEvent(askForOperationIdHandler, SpecWorkflowExternalTopics.RelayHelp);

        askForConnectionReferenceLogicalName.OnEvent(SpecWorkflowEvents.EmitError).EmitExternalEvent(askForConnectionReferenceLogicalNameHandler, SpecWorkflowExternalTopics.RelayError);
        askForConnectionReferenceLogicalName.OnEvent(SpecWorkflowEvents.EmitHelp).EmitExternalEvent(askForConnectionReferenceLogicalNameHandler, SpecWorkflowExternalTopics.RelayHelp);

        loadExistingFlow.OnEvent(SpecWorkflowEvents.EmitError).EmitExternalEvent(loadFlowHandler, SpecWorkflowExternalTopics.RelayError);
        loadExistingFlow.OnEvent(SpecWorkflowEvents.EmitHelp).EmitExternalEvent(loadFlowHandler, SpecWorkflowExternalTopics.RelayHelp);

        createSummary.OnEvent(SpecWorkflowEvents.EmitError).EmitExternalEvent(createSummaryHandler, SpecWorkflowExternalTopics.RelayError);
        createSummary.OnEvent(SpecWorkflowEvents.EmitHelp).EmitExternalEvent(createSummaryHandler, SpecWorkflowExternalTopics.RelayHelp);

        createTrigger.OnEvent(SpecWorkflowEvents.EmitError).EmitExternalEvent(createTriggerHandler, SpecWorkflowExternalTopics.RelayError);
        createTrigger.OnEvent(SpecWorkflowEvents.EmitHelp).EmitExternalEvent(createTriggerHandler, SpecWorkflowExternalTopics.RelayHelp);

        createAction.OnEvent(SpecWorkflowEvents.EmitError).EmitExternalEvent(createActionHandler, SpecWorkflowExternalTopics.RelayError);
        createAction.OnEvent(SpecWorkflowEvents.EmitHelp).EmitExternalEvent(createActionHandler, SpecWorkflowExternalTopics.RelayHelp);

        return builder.Build();
    }

    [KernelFunction("update_api_id")]
    [Description("Must be called whenever the user provides the API name. This function will use the API name to also get the API ID.")]
    public Task<string> UpdateApiNameAsync(
        [Description("The name of the API provided by the user; should begin with `shared_`. If given a full path, just provide the last part after the last `/`; the function will then resolve it to the correct API. Change any user input to title case for the API name, e.g., `shared_excelonlinebusiness` -> `shared_ExcelOnlineBusiness`.")]
        string apiName,
        [Description("The name of the connector; if not provided, it will be derived from the API name. If the API name starts with `shared_`, the connector name will be the part after `shared_`. Change any user input to title case for the Connector name (e.g., `excelonlinebusiness` -> `ExcelOnlineBusiness`).")]
        string? connectorName = null)
    {
        var input = new AskForApiNameInput
        {
            ApiName = apiName,
            ConnectorName = connectorName ?? (apiName.StartsWith("shared_") ? apiName[7..] : apiName)
        };
        return RunStepAsync(SpecWorkflowEvents.AskForApiName, input);
    }

    [KernelFunction("update_operation_id")]
    [Description("Must be called whenever the user provides the operation ID. You'll get a list of valid operation IDs from update_api_id, so always call that first before asking the user for which operation they want.")]
    public Task<string> UpdateOperationIdAsync(string operationId)
    {
        var input = new AskForOperationIdInput
        {
            OperationId = operationId
        };
        return RunStepAsync(SpecWorkflowEvents.AskForOperationId, input);
    }

    [KernelFunction("update_connection_reference_logical_name")]
    [Description("Must be called whenever the user provides the connection reference logical name. This function will validate the logical name against the Dataverse environment and update the document accordingly.")]
    public Task<string> UpdateConnectionReferenceLogicalNameAsync(string logicalName)
    {
        var input = new AskForConnectionReferenceLogicalNameInput
        {
            ConnectionReferenceLogicalName = logicalName
        };
        return RunStepAsync(SpecWorkflowEvents.AskForConnectionReferenceLogicalName, input);
    }

    private async Task<string> RunStepAsync(string eventId, object input)
    {
        var helpChannel = new HelpHandlerChannel();

        await _process.StartAsync(_kernel, new KernelProcessEvent
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

        var docJson = JsonSerializer.Serialize(_workingFlowDefinitionService.GetCurrentFlowDefinition(), _jsonOptions);
        output.Insert(0, "## Document:\n" + docJson);

        return string.Join("\n", output);
    }
}
