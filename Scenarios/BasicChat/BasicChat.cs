using OrchestrationScenarios.Models;

namespace OrchestrationScenarios.Scenarios;

public class BasicChatScenario : IScenario
{
    private readonly Agent _agent;
    public string Name => "Basic Chat Scenario";

    public BasicChatScenario(Agent agent)
    {
        _agent = agent;
    }

    public async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("<user>");

        Console.ResetColor();
        var input = "Hello";
        Console.Write(input);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("</user>");

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write("<agent>");

        Console.ResetColor();
        var reply = await _agent.ChatAsync(input ?? "");
        Console.Write($"Agent: {reply}");

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("</agent>");
        Console.ResetColor();
    }
}
