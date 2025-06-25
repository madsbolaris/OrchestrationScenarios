using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Agents.ToolChoiceBehaviors;
using AgentsSdk.Models.Agents.Models;
using AgentsSdk.Models.Tools.ToolDefinitions;

namespace AgentsSdk.Models.Runs.Requests;

/// <summary>
/// Run request for an existing agent (by ID) with optional overrides.
/// </summary>
public class AgentOverrides
{
    public string? AgentId { get; set; } = default!;

    /// <summary>
    /// Required name of the agent.
    /// </summary>
    public string? Name { get; set; } = default!;

    /// <summary>
    /// The model configuration for this agent.
    /// </summary>
    public AgentModel? Model { get; set; } = default!;

    /// <summary>
    /// Optional list of system instructions (messages) provided to the agent.
    /// </summary>
    public List<ChatMessage>? Instructions { get; set; }

    /// <summary>
    /// Optional list of tools that the agent can use.
    /// </summary>
    public List<AgentToolDefinition>? Tools { get; set; }

    /// <summary>
    /// Specifies how tools are chosen when the agent responds.
    /// </summary>
    public ToolChoiceBehavior? ToolChoice { get; set; }
}
