using System.Collections.Generic;
using ScenarioPlayer.Core.Models;

namespace ScenarioPlayer.Core.Services;

public interface IScenarioManager
{
    IReadOnlyList<ScenarioDefinition> GetAllScenarios();
    Dictionary<string, List<ScenarioDefinition>> GetScenarioTree();
}
