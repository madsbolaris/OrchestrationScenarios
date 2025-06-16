using OrchestrationScenarios.Models.ContentParts;

namespace OrchestrationScenarios.Models;

public class Message
{
    public string Name { get; set; } = "";
    public string Role { get; set; } = "user"; // user, assistant, tool, developer, system
    public List<ContentPart> Content { get; set; } = [];
}
