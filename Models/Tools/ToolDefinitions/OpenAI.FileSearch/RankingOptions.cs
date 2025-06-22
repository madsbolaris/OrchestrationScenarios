// <copyright file="RankingOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Tools.ToolDefinitions.OpenAI.FileSearch;

public class RankingOptions
{
    public float ScoreThreshold { get; set; }

    public string? Ranker { get; set; }
}
