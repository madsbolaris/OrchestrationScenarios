using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using OrchestrationScenarios.Models.Messages;
using OrchestrationScenarios.Models.Agents;
using OrchestrationScenarios.Models.Tools.ToolDefinitions.BingGrounding;
using OrchestrationScenarios.Models.Agents.Models.OpenAI;
using OrchestrationScenarios.Models.Tools.ToolDefinitions.Function;
using OrchestrationScenarios.Models.Tools.ToolDefinitions;
using OrchestrationScenarios.Models.Scenarios;
using OrchestrationScenarios.Parsing;

namespace OrchestrationScenarios.Utilities;

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
            .Build();

        var header = deserializer.Deserialize<ScenarioHeader>(yaml);


        // Create agent
        var agent = new Models.Agents.Agent
        {
            DisplayName = header.Agent.Name,
            Description = header.Agent.Description,
            Model = new OpenAIAgentModel() { Id = header.Agent.Model },
            Tools = header.Agent.Tools?.Select(t =>
            {
                return (AgentToolDefinition)(t.Name switch
                {
                    "Microsoft.BingGrounding.Search" => new BingGroundingToolDefinition(),
                    _ => (object)new FunctionToolDefinition
                    {
                        Name = t.Name,
                        Description = t.Description,
                        Method = toolMethods.TryGetValue(t.Name, out var method)
                            ? method
                            : throw new InvalidOperationException($"No method found for tool '{t.Name}'")
                    },
                });
            }).ToList()
        };

        // Parse messages
        var messages = PromptXmlParser.Parse(xml);

        return (agent, messages);
    }

    private static string JoinWithDelimiter(this string[] parts, string delimiter) =>
        string.Join(delimiter, parts);
}