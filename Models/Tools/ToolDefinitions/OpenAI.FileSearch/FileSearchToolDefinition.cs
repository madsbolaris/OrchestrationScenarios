// <copyright file="FileSearchToolDefinition.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Tools.ToolDefinitions.OpenAI.FileSearch;

public class FileSearchToolDefinition : AgentToolDefinition
{
    public override string Type => "OpenAI.FileSearch";

    public int? MaxNumResults { get; set; }

    public RankingOptions? RankingOptions { get; set; }

    public string? VectorStoreId { get; set; }
}
