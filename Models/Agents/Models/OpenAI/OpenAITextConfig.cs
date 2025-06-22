// <copyright file="OpenAITextConfig.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Agents.Models.OpenAI;

public class OpenAITextConfig
{
    public OpenAITextConfigFormat Format { get; set; }
}

public enum OpenAITextConfigFormat
{
    Text,
    JsonObject,
    JsonSchema
}
