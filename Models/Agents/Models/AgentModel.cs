using System.Text.Json;
using System.Text.Json.Serialization;
using OrchestrationScenarios.Models.Agents.Models.OpenAI;

namespace OrchestrationScenarios.Models.Agents.Models;

public abstract class AgentModel
{
    public string Id { get; set; } = default!;
    public abstract string Provider { get; }
    public string? Endpoint { get; set; }
}

public class AgentModelConverter : JsonConverter<AgentModel>
{
    public override AgentModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle string case
        if (reader.TokenType == JsonTokenType.String)
        {
            var modelId = reader.GetString();
            if (string.IsNullOrEmpty(modelId))
                throw new JsonException("Model ID cannot be null or empty.");

            return new OpenAIAgentModel
            {
                Id = modelId,
                Endpoint = null,
                Options = null
            };
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var provider = root.GetProperty("provider").GetString()?.ToLowerInvariant() ?? "azure";

        return provider switch
        {
            // "azure" => JsonSerializer.Deserialize<AzureAgentModel>(root.GetRawText(), options),
            "openai" => JsonSerializer.Deserialize<OpenAIAgentModel>(root.GetRawText(), options),
            // "generic" => JsonSerializer.Deserialize<GenericAgentModel>(root.GetRawText(), options),
            _ => throw new JsonException($"Unknown provider type: {provider}")
        };
    }

    public override void Write(Utf8JsonWriter writer, AgentModel value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
