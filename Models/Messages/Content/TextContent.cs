namespace OrchestrationScenarios.Models.Messages.Content;

using System.Collections.Generic;

public class TextContent : AIContent
{
    public override string Type => "text";

    public string Text { get; set; } = default!;

    public List<Annotation> Annotations { get; set; } = [];
}
