using AgentsSdk.Models.Agents;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using Microsoft.Extensions.Options;
using Microsoft.Agents.CopilotStudio.Client;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using AgentsSdk.Models.Messages.Content;

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
        Dictionary<string, Microsoft.Extensions.AI.AIFunction> aiFunctions = [];

        var client = new CopilotClient(
            settings: new()
            {
                EnvironmentId = settings.Value.EnvironmentId,
                SchemaName = $"{settings.Value.Prefix}_{agent.Name}"
            },
            httpClientFactory,
            logger
        );

        string? conversationId = null;

        await foreach (var _ in client.StartConversationAsync(false, cancellationToken))
        {
            if (_.Conversation is not null)
            {
                conversationId = _.Conversation.Id;
            }
        }

        conversationId ??= Guid.NewGuid().ToString();

        // check that first message is a user message
        if (messages.Count == 0 || messages.First() is not Models.Messages.Types.UserMessage)
        {
            throw new InvalidOperationException("The first message must be a user message.");
        }

        var response = client.AskQuestionAsync(
            string.Join("\n", messages.First().Content.Where(content => content is TextContent).Select(content => ((TextContent)content).Text)),
            conversationId,
            cancellationToken);

        await foreach (var update in CopilotStudioStreamingProcessor.ProcessAsync(
            response,
            conversationId!,
            messages))
        {
            yield return update;
        }
    }
}
