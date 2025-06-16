namespace OrchestrationScenarios.Models;

public abstract class Agent
{
    public string Model { get; set; } = "gpt-4";
    public List<Message> Preamble { get; set; } = new();
    public List<Message> Conclusion { get; set; } = new();
    public List<Tool> Tools { get; set; } = new();

    public abstract Task<string> ChatAsync(string input);
}
