namespace OrchestrationScenarios.Scenarios;

public interface IScenario
{
    string Name { get; }
    Task RunAsync();
}
