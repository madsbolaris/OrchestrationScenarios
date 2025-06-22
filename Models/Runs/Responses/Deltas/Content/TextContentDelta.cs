// <copyright file="TextContentDelta.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable


using System.Collections.Generic;

namespace OrchestrationScenarios.Models.Runs.Responses.Deltas.Content;

public class TextContentDelta : AIContentDelta
{
    public override string Type => "text";

    public string? Text { get; set; } = default!;

    public List<AnnotationDelta>? Annotations { get; set; }
}
