namespace AgentsSdk.Models.Tools.ToolDefinitions.PowerPlatform;

public sealed class ListEnumSettings
{
    public string OperationId { get; init; } = null!;
    public string ValueCollection { get; init; } = "value";
    public string ValuePath { get; init; } = "id";
    public string ValueTitle { get; init; } = "name";
    public object? ReadOnlyExpectedValue { get; init; }
}
