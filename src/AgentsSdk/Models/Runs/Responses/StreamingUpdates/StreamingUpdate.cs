using System.Text.Json.Serialization;
using AgentsSdk.Models.Runs.Responses.StreamingOperations;

namespace AgentsSdk.Models.Runs.Responses.StreamingUpdates;

public abstract class StreamingUpdate
{
}

/// <summary>
/// Represents a streaming update for an in-progress agent Run.
/// </summary>
public abstract class StreamingUpdate<T> : StreamingUpdate
{
    [JsonPropertyName("d")]
    public required StreamingOperation<T> Delta { get; set; }
}
