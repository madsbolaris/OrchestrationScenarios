using AgentsSdk.Models.Agents;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace ScenarioRunner.Models;

public class ScenarioHeader
{
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Agent Agent { get; set; } = new();
}

public class AgentYamlConverter() : IYamlTypeConverter
{
    private static readonly string AgentsDirectory =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Resources", "Agents"));

    public bool Accepts(Type type) => type == typeof(Agent);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
    {
        if (parser.Accept<Scalar>(out var scalar))
        {
            parser.Consume<Scalar>();
            var agentName = scalar.Value;
            var agentPath = Path.Combine(AgentsDirectory, $"{agentName}.yaml");

            if (!File.Exists(agentPath))
                throw new FileNotFoundException($"Agent definition not found at {agentPath}");

            var yamlContent = File.ReadAllText(agentPath);

            // Convert YAML → JSON → Agent using your System.Text.Json converter logic
            var intermediateObject = new DeserializerBuilder().Build().Deserialize<object>(yamlContent);
            var json = JsonSerializer.Serialize(intermediateObject);

            return JsonSerializer.Deserialize<Agent>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new JsonException("Failed to deserialize agent from resolved path.");
        }

        // Inline object deserialization using YamlDotNet
        var inlineAgent = nestedObjectDeserializer(typeof(object));
        var inlineJson = JsonSerializer.Serialize(inlineAgent);
        return JsonSerializer.Deserialize<Agent>(inlineJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer nestedObjectSerializer)
    {
        throw new NotSupportedException("Writing YAML for Agent is not supported.");
    }
}
