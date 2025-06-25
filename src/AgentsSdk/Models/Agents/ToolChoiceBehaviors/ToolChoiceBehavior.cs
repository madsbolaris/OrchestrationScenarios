namespace AgentsSdk.Models.Agents.ToolChoiceBehaviors;

/// <summary>
/// Base class for defining how tools are selected during agent operation.
/// </summary>
public abstract class ToolChoiceBehavior
{
    public abstract string Type { get; }
}
