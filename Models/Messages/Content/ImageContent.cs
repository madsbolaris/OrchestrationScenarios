// <copyright file="ImageContent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

namespace OrchestrationScenarios.Models.Messages.Content;

public class ImageContent : FileContent
{
    public override string Type => "image";

    public short? Width { get; set; }
    public short? Height { get; set; }
}
