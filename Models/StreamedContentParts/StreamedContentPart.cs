using System.ComponentModel;

namespace OrchestrationScenarios.Models.ContentParts;

public abstract class StreamedContentPart(AuthorRole authorRole, string messageId, int index)
{
    public int Index { get; set; } = index;
    public AuthorRole AuthorRole { get; set; } = authorRole;
    public string MessageId { get; set; } = messageId;
}
