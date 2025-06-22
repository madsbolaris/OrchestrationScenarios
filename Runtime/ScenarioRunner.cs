using OrchestrationScenarios.Runtime;
using OrchestrationScenarios.Utilities;

namespace OrchestrationScenarios.Runtime;

public class ScenarioRunner(string name, string path, AgentRunner runner, Dictionary<string, Delegate> tools) : IScenario
{
    public string Name => name;

    public async Task RunAsync()
    {
        var (agent, messages) = ScenarioLoader.Load(path, tools);
        await runner.RunAsync(agent, messages);
    }
}
