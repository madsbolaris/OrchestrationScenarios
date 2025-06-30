using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScenarioPlayer.Core.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using AgentsSdk.Parsing;

namespace ScenarioPlayer.Parsing;

public class YamlScenarioLoader
{
    public ScenarioDefinition Load(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var parts = content.Split("---", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            throw new InvalidOperationException("Scenario file must contain YAML frontmatter and body.");

        var yaml = parts[0].Trim();
        var xml = string.Join("---", parts[1..]).Trim();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new AgentYamlConverter())
            .Build();

        var header = deserializer.Deserialize<ScenarioHeader>(yaml);
        var messages = PromptXmlParser.Parse(xml);

        return new ScenarioDefinition
        {
            Name = Path.GetFileNameWithoutExtension(filePath).Replace('_', ' '),
            Path = filePath,
            Agent = header.Agent,
            StartingMessages = messages
        };
    }
}
