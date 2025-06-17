using OrchestrationScenarios.Models.ContentParts;

namespace OrchestrationScenarios.Models;

public class Message
{
    public string Name { get; set; } = "";
    public AuthorRole Role { get; set; } = AuthorRole.User;
    public List<ContentPart> Content { get; set; } = [];
}
