namespace AgentsSdk.Models.Conversations;

using AgentsSdk.Models.Messages;
using System.Collections.Generic;

public class Conversation
{
    public string ConversationId { get; set; } = default!;
    public long? CreatedAt { get; set; }

    public List<ChatMessage> Messages { get; set; } = new();
    public Dictionary<string, string>? Metadata { get; set; }
}
