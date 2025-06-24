using OrchestrationScenarios.Models.Messages;

namespace OrchestrationScenarios.Models.Runs.Responses;

public class Run
{
    public string AgentId { get; set; } = default!;

    public string RunId { get; set; } = default!;

    public long CreatedAt { get; set; }

    public long? CompletedAt { get; set; }

    public RunStatus Status { get; set; } = default!;

    public List<ChatMessage> Output { get; set; } = new();

    public string ConversationId { get; set; } = default!;

    public CompletionUsage? Usage { get; set; } = default!;

    public IncompleteDetails? IncompleteDetails { get; set; }
}
