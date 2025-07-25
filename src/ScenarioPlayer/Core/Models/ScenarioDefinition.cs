using System.Collections.Generic;
using AgentsSdk.Models.Agents;
using AgentsSdk.Models.Messages;

namespace ScenarioPlayer.Core.Models;

public class ScenarioDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public Agent Agent { get; init; } = new();
    public List<ChatMessage> StartingMessages { get; init; } = new();
    public string SourceFile { get; set; } = string.Empty;
}
