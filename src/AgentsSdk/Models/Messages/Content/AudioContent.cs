using System.Text.Json.Serialization;

namespace AgentsSdk.Models.Messages.Content;

public class AudioContent : FileContent
{
    [JsonPropertyName("type")]
    public override string Type => "audio";

    [JsonPropertyName("duration")]
    public short? Duration { get; set; }
}
