// <copyright file="ToolResultContent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Messages.Content;

public class ToolResultContent : AIContent
{
    public override string Type => "tool_result";

    public object? Results { get; set; }
}
