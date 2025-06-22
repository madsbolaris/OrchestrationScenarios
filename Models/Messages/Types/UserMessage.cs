// <copyright file="UserMessage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Messages.Types;

public class UserMessage : ChatMessage
{
    public string? UserId { get; set; }
}
