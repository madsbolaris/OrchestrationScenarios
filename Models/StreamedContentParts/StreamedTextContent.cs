namespace OrchestrationScenarios.Models.ContentParts;

public class StreamedTextContent(AuthorRole authorRole, string messageId, int index, string text) : StreamedContentPart(authorRole, messageId, index)
{
    public string Text { get; set; } = text;
}
