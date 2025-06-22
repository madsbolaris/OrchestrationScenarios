// <copyright file="ToolMessageDelta.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

using System.Text.Json.Serialization;

namespace OrchestrationScenarios.Models.Runs.Responses.Deltas;

public class ToolMessageDelta : SystemGeneratedMessageDelta
{
    [JsonPropertyName("r")]
    public override string Role => MessageDeltaRoles.Tool;

    [JsonPropertyName("tId")]
    public string? ToolType { get; set; }

    [JsonPropertyName("tcId")]
    public string ToolCallId { get; set; } = default!;
}
