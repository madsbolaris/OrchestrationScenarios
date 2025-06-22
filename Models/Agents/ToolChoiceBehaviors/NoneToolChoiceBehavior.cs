// <copyright file="NoneToolChoiceBehavior.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Agents.ToolChoiceBehaviors;

/// <summary>
/// Automatically selects one or more tools from the provided list.
/// </summary>
public class NoneToolChoiceBehavior : ToolChoiceBehavior
{
    public override string Type => "none";
}

