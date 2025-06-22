// <copyright file="AIContentDelta.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Runs.Responses.Deltas.Content;

public abstract class AIContentDelta
{
    public abstract string Type { get; }
}
