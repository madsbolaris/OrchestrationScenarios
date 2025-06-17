namespace OrchestrationScenarios.Models.ContentParts;

public class TextContent(string text) : ContentPart
{
    public string Text { get; set; } = text;
}
