namespace OrchestrationScenarios.Models.ContentParts;

public class StreamedStartContent(AuthorRole authorRole, string messageId) : StreamedContentPart(authorRole, messageId, -1)
{
    public AuthorRole Role { get; internal set; }
}
