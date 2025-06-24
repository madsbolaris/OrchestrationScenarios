using OrchestrationScenarios.Models.Agents;
using OrchestrationScenarios.Models.Messages;
using OrchestrationScenarios.Models.Runs.Requests.Options;


namespace OrchestrationScenarios.Models.Runs.Requests;

/// <summary>
/// Run request for an ephemeral agent configuration.
/// </summary>
public class RunRequest
{
    public Agent? Agent { get; set; }

    public List<ChatMessage> Input { get; set; } = new();

    public string? ConversationId { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }

    public RunOptions? Options { get; set; }

    public string? UserId { get; set; }
}
