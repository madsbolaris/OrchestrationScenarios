// <copyright file="AIContentUpdate.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

using System.Text.Json.Serialization;
using OrchestrationScenarios.Models.Runs.Responses.Deltas.Content;

namespace OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

/// <summary>
/// Represents a streaming update for an in-progress agent completion.
/// </summary>
public class AIContentUpdate<T> : StreamingUpdate<T> where T : AIContentDelta, new()
{
    [JsonPropertyName("mId")]
    public string MessageId { get; set; } = default!;

    [JsonPropertyName("i")]
    public int Index { get; set; } = default!;
}
