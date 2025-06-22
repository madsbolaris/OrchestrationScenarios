// <copyright file="ContentFilterContent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Messages.Content;

public class ContentFilterContent : AIContent
{
    public override string Type => "content_filter";

    public string ContentFilter { get; set; } = default!;
    public bool Detected { get; set; }
}
