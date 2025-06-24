using System.Text.Json.Serialization;

namespace OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;

/// <summary>
/// Represents an operation to delete content at a specified JSON path.
/// </summary>
public class RemoveStreamingOperation<T> : StreamingOperation<T>
{
    [JsonPropertyName("p")]
    public override string JsonPath { get; }

    [JsonPropertyName("s")]
    public int Start { get; }

    [JsonPropertyName("e")]
    public int End { get; }

    public RemoveStreamingOperation(string jsonPath)
        : this(jsonPath, 0, 0)
    {
    }

    public RemoveStreamingOperation(string jsonPath, int start, int end)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            throw new ArgumentNullException(nameof(jsonPath), "JsonPath cannot be null or empty.");
        }

        if (start < 0 || end < 0)
        {
            throw new ArgumentException("Start and End indexes must be non-negative.");
        }

        if (start > end)
        {
            throw new ArgumentException("Start index cannot be greater than End index.");
        }

        JsonPath = jsonPath;
        Start = start;
        End = end;
    }
}
