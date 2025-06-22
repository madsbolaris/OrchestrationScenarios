// <copyright file="RequiredToolChoiceBehavior.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

using System.Collections.Generic;

namespace OrchestrationScenarios.Models.Agents.ToolChoiceBehaviors;

/// <summary>
/// Automatically selects one or more tools from the provided list.
/// </summary>
public class RequiredToolChoiceBehavior : ToolChoiceBehavior
{
    public override string Type => "auto";

    public List<string> ToolNames { get; set; } = [];
}
