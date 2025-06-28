using AgentsSdk.Models.Agents;
using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using AgentsSdk.Runtime;
using AgentsSdk.Runtime.Streaming.Providers.CopilotStudio;
using AgentsSdk.Runtime.Streaming.Providers.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using ScenarioRunner.Helpers;
using ScenarioRunner.Interfaces;

namespace ScenarioRunner.Core;

public class ScenarioRunner : IScenario
{
    private readonly AgentRunner<OpenAIStreamingClient> openAIClient;
    private readonly AgentRunner<CopilotStudioStreamingClient> copilotStudioClient;
    private readonly string _name;
    private readonly string _path;
    private readonly Dictionary<string, Delegate> _tools;

    private Agent? _agent;
    private List<ChatMessage>? _messages;

    public string Name => _name;

    public ScenarioRunner(IServiceProvider sp, string name, string path, Dictionary<string, Delegate> tools)
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

    public IAsyncEnumerable<StreamingUpdate> RunOpenAIStream(CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        var clonedMessages = _messages!.Select(m => m.Clone()).ToList();
        return openAIClient.RunAsync(_agent!, clonedMessages, cancellationToken);
    }

    public IAsyncEnumerable<StreamingUpdate> RunCopilotStudioStream(CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        var clonedMessages = _messages!.Select(m => m.Clone()).ToList();
        return copilotStudioClient.RunAsync(_agent!, clonedMessages, cancellationToken);
    }


}
