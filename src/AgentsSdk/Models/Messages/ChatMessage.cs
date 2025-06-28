using System.Text.Json;
using System.Text.Json.Serialization;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Runs.Responses;

namespace AgentsSdk.Models.Messages;

[JsonConverter(typeof(ChatMessageConverter))]
public abstract class ChatMessage
{
    private static readonly JsonSerializerOptions CloneSerializerOptions = new()
    {
        Converters = { new ChatMessageConverter(), new AIContentConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; } = default!;

    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; } = default!;

    [JsonPropertyName("content")]
    public List<AIContent> Content { get; set; } = default!;

    [JsonPropertyName("createdAt")]
    public long? CreatedAt { get; set; }

    public ChatMessage Clone()
    {
        var json = JsonSerializer.Serialize(this, CloneSerializerOptions);
        return JsonSerializer.Deserialize<ChatMessage>(json, CloneSerializerOptions)!;
    }
}


public class ChatMessageConverter : JsonConverter<ChatMessage>
{
    public override ChatMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("role", out var roleProp))
            throw new JsonException("Missing 'role' property.");

        var role = roleProp.GetString()?.ToLowerInvariant();
        ChatMessage message = role switch
        {
            "system" => new SystemMessage(),
            "developer" => new DeveloperMessage(),
            "user" => new UserMessage(),
            "agent" => new AgentMessage(),
            "tool" => new ToolMessage(),
            _ => throw new JsonException($"Unknown role '{role}'")
        };

        if (root.TryGetProperty("messageId", out var msgId))
            message.MessageId = msgId.GetString();

        if (root.TryGetProperty("conversationId", out var convId))
            message.ConversationId = convId.GetString();

        if (root.TryGetProperty("createdAt", out var created))
            message.CreatedAt = created.GetInt64();

        if (root.TryGetProperty("content", out var contentProp))
        {
            message.Content = contentProp.ValueKind switch
            {
                JsonValueKind.String => new List<AIContent>
                {
                    new TextContent
                    {
                        Text = contentProp.GetString() ?? "",
                        Annotations = []
                    }
                },
                JsonValueKind.Array => JsonSerializer.Deserialize<List<AIContent>>(contentProp.GetRawText(), new JsonSerializerOptions
                {
                    Converters = { new AIContentConverter() }
                }) ?? [],
                _ => throw new JsonException("Invalid 'content' type. Expected string or array.")
            };
        }

        if (message is SystemGeneratedMessage sys)
        {
            if (root.TryGetProperty("runId", out var runId))
                sys.RunId = runId.GetString();

            if (root.TryGetProperty("completedAt", out var completedAt))
                sys.CompletedAt = completedAt.GetInt64();

            if (root.TryGetProperty("usage", out var usage))
                sys.Usage = JsonSerializer.Deserialize<CompletionUsage>(usage.GetRawText(), options);
        }

        if (message is AgentMessage agent && root.TryGetProperty("agentId", out var agentId))
            agent.AgentId = agentId.GetString();

        if (message is ToolMessage tool && root.TryGetProperty("toolType", out var toolType))
            tool.ToolType = toolType.GetString()!;

        return message;
    }

    public override void Write(Utf8JsonWriter writer, ChatMessage value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Role
        writer.WriteString("role", value switch
        {
            SystemMessage => "system",
            DeveloperMessage => "developer",
            UserMessage => "user",
            AgentMessage => "agent",
            ToolMessage => "tool",
            _ => throw new JsonException($"Unsupported message type: {value.GetType().Name}")
        });

        // Content
        writer.WritePropertyName("content");
        JsonSerializer.Serialize(writer, value.Content, options);

        if (!string.IsNullOrEmpty(value.MessageId))
            writer.WriteString("messageId", value.MessageId);

        if (!string.IsNullOrEmpty(value.ConversationId))
            writer.WriteString("conversationId", value.ConversationId);

        if (value.CreatedAt.HasValue)
            writer.WriteNumber("createdAt", value.CreatedAt.Value);

        if (value is SystemGeneratedMessage sys)
        {
            if (!string.IsNullOrEmpty(sys.RunId))
                writer.WriteString("runId", sys.RunId);

            if (sys.CompletedAt.HasValue)
                writer.WriteNumber("completedAt", sys.CompletedAt.Value);

            if (sys.Usage is not null)
            {
                writer.WritePropertyName("usage");
                JsonSerializer.Serialize(writer, sys.Usage, options);
            }
        }

        if (value is AgentMessage agent && !string.IsNullOrEmpty(agent.AgentId))
            writer.WriteString("agentId", agent.AgentId);

        if (value is ToolMessage tool && !string.IsNullOrEmpty(tool.ToolType))
            writer.WriteString("toolType", tool.ToolType);

        writer.WriteEndObject();
    }

}
