// <copyright file="ChatMessageUpdate.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

using System.Text.Json.Serialization;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;

namespace OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

/// <summary>
/// Represents a streaming update for an in-progress agent completion.
/// </summary>
public class ChatMessageUpdate : StreamingUpdate<SystemGeneratedMessageDelta>
{
    [JsonPropertyName("mId")]
    public string MessageId { get; set; } = default!;

    [JsonPropertyName("cId")]
    public string ConversationId { get; set; } = default!;

    [JsonPropertyName("u")]
    public CompletionUsage? Usage { get; set; } = default!;
}
