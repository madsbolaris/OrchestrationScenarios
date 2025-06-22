// <copyright file="AgentToolDefinition.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using OrchestrationScenarios.Models.Tools.ToolDefinitions.BingGrounding;

namespace OrchestrationScenarios.Models.Tools.ToolDefinitions;

[JsonConverter(typeof(AgentToolDefinitionConverter))]
public abstract class AgentToolDefinition
{
    public abstract string Type { get; }

    public ToolOverrides? Override { get; set; }
}

public class ToolOverrides
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public JsonNode? Parameters { get; set; }
}

public class AgentToolDefinitionConverter : JsonConverter<AgentToolDefinition>
{
    public override AgentToolDefinition? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

        return type switch
        {
            "Microsoft.BingGrounding" => JsonSerializer.Deserialize<BingGroundingToolDefinition>(json, options),
            // Add more tool definitions here
            _ => throw new JsonException($"Unknown tool type '{type}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, AgentToolDefinition value, JsonSerializerOptions options)
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

