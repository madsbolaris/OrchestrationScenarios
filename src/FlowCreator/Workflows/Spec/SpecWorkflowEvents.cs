// Copyright (c) Microsoft. All rights reserved.
namespace Mauve.Workflows.GenerateEmailMarkdown;

/// <summary>
/// Processes Events emitted by shared steps.<br/>
/// </summary>
public static class SpecWorkflowEvents
{
    public static readonly string Start = nameof(Start);
    public static readonly string AskForApiId = nameof(AskForApiId);
}