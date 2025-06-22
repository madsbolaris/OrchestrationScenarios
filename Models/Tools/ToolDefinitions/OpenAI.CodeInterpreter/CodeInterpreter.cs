// <copyright file="CodeInterpreterToolDefinition.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Tools.ToolDefinitions.OpenAI.CodeInterpreter;

public class CodeInterpreterToolDefinition : AgentToolDefinition
{
    public override string Type => "OpenAI.CodeInterpreter";

    public List<string>? FileIds { get; set; }
}
