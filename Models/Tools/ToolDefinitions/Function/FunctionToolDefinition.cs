// <copyright file="FunctionToolDefinition.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

using System.Text.Json.Nodes;

namespace OrchestrationScenarios.Models.Tools.ToolDefinitions.Function;

public class FunctionToolDefinition : AgentToolDefinition
{
    public override string Type => "function";

    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    /// <summary>
    /// JSON Schema describing the parameters. Stored as raw JSON.
    /// </summary>
    public JsonNode? Parameters { get; set; }

    public bool? Strict { get; set; }

    public Delegate? Method { get; set; }
}
