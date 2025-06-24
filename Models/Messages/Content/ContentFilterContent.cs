namespace OrchestrationScenarios.Models.Messages.Content;

public class ContentFilterContent : AIContent
{
    public override string Type => "content_filter";

    public string ContentFilter { get; set; } = default!;
    public bool Detected { get; set; }
}
