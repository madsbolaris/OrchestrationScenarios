// <copyright file="OpenAIAgentModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Agents.Models.OpenAI
{
    public class OpenAIAgentModel : AgentModel
    {
        public override string Provider => "openai";

        public OpenAIModelOptions? Options { get; set; } = default!;
    }
}
