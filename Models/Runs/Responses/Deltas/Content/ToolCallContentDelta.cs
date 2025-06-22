// <copyright file="TextContentDelta.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable


using System.Collections.Generic;

namespace OrchestrationScenarios.Models.Runs.Responses.Deltas.Content;

public class ToolCallContentDelta : AIContentDelta
{
    public override string Type => "tool_call";

    public string? Arguments { get; set; } = default!;
}
