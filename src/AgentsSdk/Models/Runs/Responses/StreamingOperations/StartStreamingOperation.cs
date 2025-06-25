using System.Text.Json.Serialization;

namespace AgentsSdk.Models.Runs.Responses.StreamingOperations;

public class StartStreamingOperation<T>(T value) : StreamingOperation<T>
{
    private readonly T _value = value;

    [JsonPropertyName("v")]
    public object? Value => _value;
}
