using OrchestrationScenarios.Models.Runs.Responses;

namespace OrchestrationScenarios.Models.Messages;

public abstract class SystemGeneratedMessage : ChatMessage
{
    public string? RunId { get; set; }
    public long? CompletedAt { get; set; }
    public CompletionUsage? Usage { get; set; } = default!;
}
