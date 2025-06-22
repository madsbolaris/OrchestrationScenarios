// <copyright file="AgentMessage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Messages.Types;

public class AgentMessage : SystemGeneratedMessage
{
    public string? AgentId { get; set; }
}
