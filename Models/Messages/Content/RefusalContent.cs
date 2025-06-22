// <copyright file="RefusalContent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Messages.Content;

public class RefusalContent : AIContent
{
    public override string Type => "refusal";

    public string Refusal { get; set; } = default!;
}
