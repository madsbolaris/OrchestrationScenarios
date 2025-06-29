using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AgentsSdk.Models.Tools.ToolDefinitions.BingGrounding;
using AgentsSdk.Models.Tools.ToolDefinitions.PowerPlatform;

namespace AgentsSdk.Models.Tools.ToolDefinitions;

[JsonConverter(typeof(ToolDefinitionConverter))]
public abstract class ToolDefinition
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    [JsonPropertyName("overrides")]
    public virtual ToolOverrides? Overrides { get; set; }
}

public class ToolOverrides
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    public JsonNode? Parameters { get; set; }
}

public class ToolDefinitionConverter : JsonConverter<ToolDefinition>
{
    public override ToolDefinition? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp))
        {
            throw new JsonException("Missing 'type' discriminator.");
        }

        var type = typeProp.GetString();
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new JsonException("'type' discriminator must be a non-empty string.");
        }

        var json = root.GetRawText();

        ToolDefinition tool = type switch
        {
            "Microsoft.BingGrounding" =>
                JsonSerializer.Deserialize<BingGroundingToolDefinition>(json, options)!,

            _ when type.StartsWith("Microsoft.PowerPlatform") =>
                new PowerPlatformToolDefinition(type),

            _ => throw new JsonException($"Unknown tool type '{type}'")
        };

        // Handle tool overrides
        if (root.TryGetProperty("overrides", out var overridesProp))
        {
            var overrides = JsonSerializer.Deserialize<ToolOverrides>(overridesProp.GetRawText(), options);
            tool.Overrides = overrides;
        }

        return tool;
    }

    public override void Write(Utf8JsonWriter writer, ToolDefinition value, JsonSerializerOptions options)
    {
        var type = value.Type;
        using var doc = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), options));
        writer.WriteStartObject();

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            prop.WriteTo(writer);
        }

        writer.WriteString("type", type);
        writer.WriteEndObject();
    }
}

