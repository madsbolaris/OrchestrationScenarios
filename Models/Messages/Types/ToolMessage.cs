// <copyright file="ToolMessage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Messages.Types;

public class ToolMessage : SystemGeneratedMessage
{
    public string ToolType { get; set; } = default!;

    public string ToolCallId { get; set; } = default!;
}
