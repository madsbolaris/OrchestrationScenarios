namespace AgentsSdk.Models.Runs.Responses.Deltas.Content;

public class TextContentDelta : AIContentDelta
{
    public override string Type => "text";

    public string? Text { get; set; } = default!;

    public List<AnnotationDelta>? Annotations { get; set; }
}
