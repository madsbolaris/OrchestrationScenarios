// <copyright file="StreamingOperation.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

using System.Text.Json.Serialization;

namespace OrchestrationScenarios.Models.Runs.Responses.StreamingOperations
{
    public abstract class StreamingOperation<T>
    {
        [JsonPropertyName("p")]
        public virtual string? JsonPath { get; }
    }
}
