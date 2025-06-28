using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Agents;
using AgentsSdk.Models.Tools.ToolDefinitions.BingGrounding;
using AgentsSdk.Models.Agents.Models.OpenAI;
using AgentsSdk.Models.Tools.ToolDefinitions.Function;
using AgentsSdk.Models.Tools.ToolDefinitions;
using AgentsSdk.Parsing;
using ScenarioRunner.Models;

namespace ScenarioRunner.Helpers;

public static class ScenarioLoader
{
    public static (Agent agent, List<ChatMessage> messages) Load(string liquidPath, Dictionary<string, Delegate> toolMethods)
    {
        var content = File.ReadAllText(liquidPath);

        // Split frontmatter (YAML) and body (XML)
        var parts = content.Split("---", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            throw new InvalidOperationException("Liquid file must contain YAML frontmatter and XML body.");

        string yaml = parts[0].Trim();
        string xml = parts[1..].JoinWithDelimiter("---").Trim();

        // Deserialize YAML
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new AgentYamlConverter())
            .Build();

        var header = deserializer.Deserialize<ScenarioHeader>(yaml);
        
        // Parse messages
        var messages = PromptXmlParser.Parse(xml);

        return (header.Agent, messages);
    }

    private static string JoinWithDelimiter(this string[] parts, string delimiter) =>
        string.Join(delimiter, parts);
}