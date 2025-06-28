using System.Text.Json.Serialization;

namespace AgentsSdk.Models.Messages.Content;

public class VideoContent : FileContent
{
    [JsonPropertyName("type")]
    public override string Type => "video";

    [JsonPropertyName("duration")]
    public short? Duration { get; set; }

    [JsonPropertyName("width")]
    public short? Width { get; set; }

    [JsonPropertyName("height")]
    public short? Height { get; set; }
}
