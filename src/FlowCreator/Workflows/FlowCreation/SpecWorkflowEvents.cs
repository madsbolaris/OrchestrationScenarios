// Copyright (c) Microsoft. All rights reserved.
namespace FlowCreator.Workflows.FlowCreation;

/// <summary>
/// Processes Events emitted by shared steps.<br/>
/// </summary>
public static class SpecWorkflowEvents
{
    public static readonly string AskForApiName = nameof(AskForApiName);
    public static readonly string AskForOperationId = nameof(AskForOperationId);
    public static readonly string AskForConnectionReferenceLogicalName = nameof(AskForConnectionReferenceLogicalName);
    public static readonly string LoadExistingFlow = nameof(LoadExistingFlow);
    public static readonly string CreateTrigger = nameof(CreateTrigger);
    public static readonly string CreateAction = nameof(CreateAction);
    public static readonly string SaveFlow = nameof(SaveFlow);
    public static readonly string EmitError = nameof(EmitError);
    public static readonly string EmitHelp = nameof(EmitHelp);
}