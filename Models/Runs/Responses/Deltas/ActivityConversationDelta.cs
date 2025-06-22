// <copyright file="ConversationDelta.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

using System.Text.Json.Serialization;
using OrchestrationScenarios.Models.Messages;

namespace OrchestrationScenarios.Models.Runs.Responses.Deltas;

public class ActivityConversationDelta
{
    [JsonPropertyName("m")]
    public List<ChatMessage>? Messages { get; set; }
}
