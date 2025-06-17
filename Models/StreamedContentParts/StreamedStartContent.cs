namespace OrchestrationScenarios.Models.ContentParts;

public class StreamedStartContent(AuthorRole authorRole, string messageId) : StreamedContentPart(authorRole, messageId, -1)
{
}
