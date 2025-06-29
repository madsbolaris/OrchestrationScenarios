using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using AgentsSdk.Runtime.Streaming;
using ScenarioPlayer.Core.Models;

namespace ScenarioPlayer.Core.Services;

public class ScenarioExecutor
{
    private readonly IEnumerable<IStreamingAgentClient> _clients;

    public ScenarioExecutor(IEnumerable<IStreamingAgentClient> clients)
    {
        _clients = clients;
    }

    public Dictionary<string, IAsyncEnumerable<StreamingUpdate>> RunScenario(
        ScenarioDefinition scenario,
        CancellationToken cancellationToken = default)
    {
        return _clients.ToDictionary(
            client => client.GetType().Name.Replace("StreamingClient", ""),
            client => client.RunStreamingAsync(
                scenario.Agent,
                scenario.StartingMessages.Select(m => m.Clone()).ToList(),
                cancellationToken
            )
        );
    }
}
