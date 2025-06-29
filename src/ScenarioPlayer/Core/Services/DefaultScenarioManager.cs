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

    public DefaultScenarioManager(YamlScenarioLoader loader)
    {
        _loader = loader;

        var scenarioDir = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Scenarios");
        if (Directory.Exists(scenarioDir))
        {
            foreach (var file in Directory.EnumerateFiles(scenarioDir, "*.liquid", SearchOption.AllDirectories))
            {
                var scenario = _loader.Load(file);
                _scenarios.Add(scenario);
            }
        }
    }

    public IReadOnlyList<ScenarioDefinition> GetAllScenarios() => _scenarios;
}
