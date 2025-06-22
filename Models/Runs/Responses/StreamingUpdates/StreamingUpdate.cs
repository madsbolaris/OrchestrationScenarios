// <copyright file="StreamingUpdate.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

using System.Text.Json.Serialization;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;

namespace OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

public abstract class StreamingUpdate
{
}

/// <summary>
/// Represents a streaming update for an in-progress agent Run.
/// </summary>
public abstract class StreamingUpdate<T> : StreamingUpdate
{
    [JsonPropertyName("d")]
    public StreamingOperation<T>? Delta { get; set; }
}
