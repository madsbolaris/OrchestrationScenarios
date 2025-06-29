using System.Text.Json;
using System.Text.Json.Serialization;
using AgentsSdk.Models.Agents.Models;
using AgentsSdk.Models.Agents.ToolChoiceBehaviors;
using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Tools.ToolDefinitions;

namespace AgentsSdk.Models.Agents;

[JsonConverter(typeof(BaseAgentConverter<BaseAgent>))]
public class BaseAgent
{
    public string? Name { get; set; } = default!;
    public string? DisplayName { get; set; } = default!;
    public string? Description { get; set; }
    public AgentModel Model { get; set; } = default!;
    public List<ChatMessage>? Instructions { get; set; }
    public List<ToolDefinition>? Tools { get; set; }
    public ToolChoiceBehavior? ToolChoice { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}


public class BaseAgentConverter<T> : JsonConverter<T> where T : BaseAgent, new()
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var modelJson = root.GetProperty("model").GetRawText();
        var model = JsonSerializer.Deserialize<AgentModel>(modelJson, new JsonSerializerOptions
        {
            Converters = { new AgentModelConverter() },
            PropertyNameCaseInsensitive = true
        }) ?? throw new JsonException("Missing required property 'model'");

        var agent = new T
        {
            Name = root.GetProperty("name").GetString() ?? throw new JsonException("Missing required property 'name'"),
            DisplayName = root.GetProperty("displayName").GetString() ?? null,
            Description = root.TryGetProperty("description", out var descProp) ? descProp.GetString() : null,
            Model = model,
            Instructions = ParseInstructions(root, options),
            Tools = root.TryGetProperty("tools", out var toolsProp)
                ? JsonSerializer.Deserialize<List<ToolDefinition>>(toolsProp.GetRawText(), new JsonSerializerOptions
                {
                    Converters = { new ToolDefinitionConverter() }
                })
                : new List<ToolDefinition>(),
            ToolChoice = root.TryGetProperty("toolChoice", out var toolChoiceProp)
                ? JsonSerializer.Deserialize<ToolChoiceBehavior>(toolChoiceProp.GetRawText(), options)
                : null,
            Metadata = root.TryGetProperty("metadata", out var metadataProp)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(metadataProp.GetRawText(), options)
                : null
        };

        if (agent is Agent derived && root.TryGetProperty("agentId", out var idProp))
        {
            derived.AgentId = idProp.GetString()!;
        }

        return agent;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("name", value.Name);
        
        if (!string.IsNullOrWhiteSpace(value.DisplayName))
            writer.WriteString("displayName", value.DisplayName);

        if (!string.IsNullOrWhiteSpace(value.Description))
            writer.WriteString("description", value.Description);

        writer.WritePropertyName("model");
        JsonSerializer.Serialize(writer, value.Model, value.Model.GetType(), options);

        if (value.Instructions != null)
        {
            writer.WritePropertyName("instructions");
            JsonSerializer.Serialize(writer, value.Instructions, options);
        }

        writer.WritePropertyName("tools");
        JsonSerializer.Serialize(writer, value.Tools ?? new List<ToolDefinition>(), options);

        if (value.ToolChoice != null)
        {
            writer.WritePropertyName("toolChoice");
            JsonSerializer.Serialize(writer, value.ToolChoice, options);
        }

        if (value.Metadata != null)
        {
            writer.WritePropertyName("metadata");
            JsonSerializer.Serialize(writer, value.Metadata, options);
        }

        if (value is Agent derived)
        {
            writer.WriteString("agentId", derived.AgentId);
        }

        writer.WriteEndObject();
    }

    private List<ChatMessage>? ParseInstructions(JsonElement root, JsonSerializerOptions options)
    {
        if (!root.TryGetProperty("instructions", out var token))
            return null;

        if (token.ValueKind == JsonValueKind.String)
        {
            return new List<ChatMessage>
            {
                new SystemMessage
                {
                    Content = new List<AIContent>
                    {
                        new TextContent
                        {
                            Text = token.GetString() ?? string.Empty,
                            Annotations = []
                        }
                    }
                }
            };
        }

        if (token.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<List<ChatMessage>>(token.GetRawText(), options);
        }

        return null;
    }
}
