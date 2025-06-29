namespace AgentsSdk.Models.Tools;

using System.Text.Json.Nodes;

internal sealed class ToolMetadata
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? Description { get; init; }
    public JsonNode? Parameters { get; init; }

    /// <summary>
    /// Optional runtime executor for this tool. Null if not executable in current context.
    /// </summary>
    public Func<Dictionary<string, object?>, Task<object?>>? Executor { get; set; }
}
