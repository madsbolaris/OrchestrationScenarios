using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentsSdk.Models.Messages.Content;

public abstract class AIContent
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

public class AIContentConverter : JsonConverter<AIContent>
{
    public override AIContent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            return new TextContent
            {
                Text = text ?? string.Empty,
                Annotations = []
            };
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp))
        {
            throw new JsonException("Missing 'type' property for AIContent.");
        }

        var type = typeProp.GetString()?.ToLowerInvariant();
        var rawJson = root.GetRawText();

        return type switch
        {
            "text" => JsonSerializer.Deserialize<TextContent>(rawJson, options),
            "image" => JsonSerializer.Deserialize<ImageContent>(rawJson, options),
            "audio" => JsonSerializer.Deserialize<AudioContent>(rawJson, options),
            "content_filter" => JsonSerializer.Deserialize<ContentFilterContent>(rawJson, options),
            "file" => JsonSerializer.Deserialize<FileContent>(rawJson, options),
            "refusal" => JsonSerializer.Deserialize<RefusalContent>(rawJson, options),
            "tool_call" => JsonSerializer.Deserialize<ToolCallContent>(rawJson, options),
            "tool_result" => JsonSerializer.Deserialize<ToolResultContent>(rawJson, options),
            "video" => JsonSerializer.Deserialize<VideoContent>(rawJson, options),
            _ => throw new JsonException($"Unknown AIContent type: {type}")
        };
    }

    public override void Write(Utf8JsonWriter writer, AIContent value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
