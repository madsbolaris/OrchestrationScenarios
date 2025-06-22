// <copyright file="TextContentDelta.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable


using System.Collections.Generic;

namespace OrchestrationScenarios.Models.Runs.Responses.Deltas.Content;

public class ToolResultContentDelta : AIContentDelta
{
    public override string Type => "tool_result";

    public string? Result { get; set; } = default!;
}
