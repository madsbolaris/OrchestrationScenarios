using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScenarioPlayer.Core.Models;
using ScenarioPlayer.Parsing;

namespace ScenarioPlayer.Core.Services;

public class DefaultScenarioManager : IScenarioManager
{
    private readonly List<ScenarioDefinition> _scenarios = [];
    private readonly YamlScenarioLoader _loader;
    private readonly string _scenarioRoot;

    public DefaultScenarioManager(YamlScenarioLoader loader)
    {
        _loader = loader;

        _scenarioRoot = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Scenarios");
        if (Directory.Exists(_scenarioRoot))
        {
            foreach (var file in Directory.EnumerateFiles(_scenarioRoot, "*.liquid", SearchOption.AllDirectories))
            {
                var scenario = _loader.Load(file);
                scenario.SourceFile = file; // Ensure this is set so we can use it below
                _scenarios.Add(scenario);
            }
        }
    }

    public IReadOnlyList<ScenarioDefinition> GetAllScenarios() => _scenarios;

    public Dictionary<string, List<ScenarioDefinition>> GetScenarioTree()
    {
        return _scenarios
            .GroupBy(s => GetRelativeFolder(s.SourceFile))
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Name).ToList());
    }

    private string GetRelativeFolder(string filePath)
    {
        var relativePath = Path.GetRelativePath(_scenarioRoot, Path.GetDirectoryName(filePath) ?? "");
        return string.IsNullOrWhiteSpace(relativePath) ? "Root" : relativePath;
    }
}
