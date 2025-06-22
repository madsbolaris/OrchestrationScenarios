// <copyright file="BingGroundingToolDefinition.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Tools.ToolDefinitions.BingGrounding;

public class BingGroundingToolDefinition : AgentToolDefinition
{
    public override string Type => "Microsoft.BingGrounding";
    public string ConnectionName { get; set; } = null!;
}
