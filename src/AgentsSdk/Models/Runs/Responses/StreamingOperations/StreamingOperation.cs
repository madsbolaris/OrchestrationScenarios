using System.Text.Json.Serialization;

namespace AgentsSdk.Models.Runs.Responses.StreamingOperations
{
    public abstract class StreamingOperation<T>
    {
        [JsonPropertyName("p")]
        public virtual string? JsonPath { get; }
    }
}
