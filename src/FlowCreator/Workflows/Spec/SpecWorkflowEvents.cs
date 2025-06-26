// Copyright (c) Microsoft. All rights reserved.
namespace FlowCreator.Workflows.Spec;

/// <summary>
/// Processes Events emitted by shared steps.<br/>
/// </summary>
public static class SpecWorkflowEvents
{
    public static readonly string AskForApiId = nameof(AskForApiId);
    public static readonly string AskForOperationId = nameof(AskForOperationId);
    public static readonly string EmitError = nameof(EmitError);
}