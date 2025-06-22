namespace OrchestrationScenarios.Runtime;

public interface IScenario
{
    string Name { get; }
    Task RunAsync();
}
