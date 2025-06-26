// Copyright (c) Microsoft. All rights reserved.
namespace FlowCreator.Workflows.Spec;

/// <summary>
/// Processes Events emitted by shared steps.<br/>
/// </summary>
public static class SpecWorkflowExternalTopics
{
    public static readonly string RelayError = nameof(RelayError);
    public static readonly string RelayHelp = nameof(RelayHelp);
}