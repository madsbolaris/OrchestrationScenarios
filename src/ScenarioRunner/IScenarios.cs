namespace ScenarioRunner;

public interface IScenario
{
    string Name { get; }
    Task RunOpenAIAsync();
    Task RunCopilotStudioAsync();
}
