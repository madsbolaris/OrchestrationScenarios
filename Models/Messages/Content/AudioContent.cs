// <copyright file="AudioContent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Messages.Content;

public class AudioContent : FileContent
{
    public override string Type => "audio";

    public short? Duration { get; set; }
}
