namespace OrchestrationScenarios.Models.ContentParts;

public class StreamedFunctionResultContent(
    string messageId,
    string callId,
    int index,
    string result
) : StreamedContentPart(AuthorRole.Tool, messageId, index)
{
    public string Result { get; set; } = result;
    public string CallId { get; set; } = callId;
}
