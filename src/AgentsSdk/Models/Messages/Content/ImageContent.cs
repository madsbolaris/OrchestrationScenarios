using System.Text.Json.Serialization;

namespace AgentsSdk.Models.Messages.Content;

public class ImageContent : FileContent
{
    [JsonPropertyName("type")]
    public override string Type => "image";

    [JsonPropertyName("width")]
    public short? Width { get; set; }

    [JsonPropertyName("height")]
    public short? Height { get; set; }
}
