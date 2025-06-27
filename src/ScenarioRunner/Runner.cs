using AgentsSdk.Models.Agents;
using AgentsSdk.Runtime;
using AgentsSdk.Runtime.Streaming.Providers.CopilotStudio;
using AgentsSdk.Runtime.Streaming.Providers.OpenAI;
using Microsoft.Extensions.DependencyInjection;

namespace ScenarioRunner;

public class Runner : IScenario
{
    private readonly AgentRunner<OpenAIStreamingClient> openAIClient;
    private readonly AgentRunner<CopilotStudioStreamingClient> copilotStudioClient;
    private readonly string _name;
    private readonly string _path;
    private readonly Dictionary<string, Delegate> _tools;

    private Agent? _agent;
    private List<AgentsSdk.Models.Messages.ChatMessage>? _messages;

    public string Name => _name;

    public Runner(IServiceProvider sp, string name, string path, Dictionary<string, Delegate> tools)
    {
        _name = name;
        _path = path;
        _tools = tools;

        openAIClient = sp.GetRequiredService<AgentRunner<OpenAIStreamingClient>>();
        copilotStudioClient = sp.GetRequiredService<AgentRunner<CopilotStudioStreamingClient>>();
    }

    private void EnsureLoaded()
    {
        if (_agent == null || _messages == null)
        {
            (_agent, _messages) = ScenarioLoader.Load(_path, _tools);
        }
    }

    public async Task RunOpenAIAsync()
    {
        EnsureLoaded();
        await openAIClient.RunAsync(_agent!, _messages!);
    }

    public async Task RunCopilotStudioAsync()
    {
        EnsureLoaded();
        await copilotStudioClient.RunAsync(_agent!, _messages!);
    }
}
