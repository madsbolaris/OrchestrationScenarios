using System.Text.Json.Serialization;

namespace AgentsSdk.Models.Messages.Content;

public class FileContent : AIContent
{
    [JsonPropertyName("type")]
    public override string Type => "file";

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileSize")]
    public string? MimeType { get; set; }

    [JsonPropertyName("mimeType")]
    public string? Uri { get; set; }

    [JsonPropertyName("dataUri")]
    public string? DataUri { get; set; }

    [JsonPropertyName("data")]
    public byte[]? Data { get; set; }
}
