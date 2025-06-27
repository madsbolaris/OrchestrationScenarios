using Microsoft.Extensions.AI;
using AgentsSdk.Models.Agents;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using Microsoft.Extensions.Options;
using Microsoft.Agents.CopilotStudio.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Agents.Core.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AgentsSdk.Runtime.Streaming.Providers.CopilotStudio;

public sealed class CopilotStudioStreamingClient(
    IOptions<DataverseSettings> settings,
    IHttpClientFactory httpClientFactory,
    ILogger<CopilotStudioStreamingClient> logger) : IStreamingAgentClient
{
    public async IAsyncEnumerable<StreamingUpdate> RunStreamingAsync(
        Agent agent,
        List<Models.Messages.ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Dictionary<string, AIFunction> aiFunctions = [];

        var client = new CopilotClient(
            settings: new()
            {
                EnvironmentId = settings.Value.EnvironmentId,
                SchemaName = "mabolan_Agent-ExcelOnlineBusiness-GetItem"
            },
            httpClientFactory,
            logger
        );

        await foreach (var _ in client.StartConversationAsync(false, cancellationToken))
        {
        }

        var response = client.AskQuestionAsync("""
            Give me the value of this row
            - source: me
            - drive: b!weWC2WCKw0Cpsgu5Y_wRbWkAT9ROadtMtivYyfycom8cje9vd6I7TKSmuFgFyOa3
            - file: 01BDXC5B2PAD276RNNFRBJ7QSG7PLXA5G3
            - table: {7768D97E-C6EB-4A6D-9D81-7FAF86AF1894}
            - idColumn: ID
            - id: 1
            """, "fa599c0b-8d6c-4188-abd8-f345fac7cf0b", cancellationToken);
        var conversationId = messages.FirstOrDefault()?.ConversationId ?? Guid.NewGuid().ToString();

        await foreach (var update in CopilotStudioStreamingProcessor.ProcessAsync(
            response,
            conversationId,
            messages))
        {
            yield return update;
        }
    }
}
