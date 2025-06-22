// <copyright file="Thread.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Conversations;

using OrchestrationScenarios.Models.Messages;
using System.Collections.Generic;

public class Conversation
{
    public string ConversationId { get; set; } = default!;
    public long? CreatedAt { get; set; }

    public List<ChatMessage> Messages { get; set; } = new();
    public Dictionary<string, string>? Metadata { get; set; }
}
