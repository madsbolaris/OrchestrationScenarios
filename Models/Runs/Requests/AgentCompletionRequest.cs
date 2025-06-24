using OrchestrationScenarios.Models.Messages;
using OrchestrationScenarios.Models.Runs.Requests.Options;

namespace OrchestrationScenarios.Models.Runs.Requests;

/// <summary>
/// Run request for an existing agent (by ID) with optional overrides.
/// </summary>
public class AgentRunRequest
{
    public string AgentId { get; set; } = default!;

    public AgentOverrides Overrides { get; set; } = default!;

    public List<ChatMessage> Input { get; set; } = new();

    public string? ThreadId { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }

    public RunOptions? Options { get; set; }

    public string? UserId { get; set; }

    public bool? Store { get; set; }
}
