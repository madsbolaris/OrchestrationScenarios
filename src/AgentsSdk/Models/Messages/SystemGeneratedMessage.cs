using AgentsSdk.Models.Runs.Responses;

namespace AgentsSdk.Models.Messages;

public abstract class SystemGeneratedMessage : ChatMessage
{
    public string? RunId { get; set; }
    public long? CompletedAt { get; set; }
    public CompletionUsage? Usage { get; set; } = default!;
}
