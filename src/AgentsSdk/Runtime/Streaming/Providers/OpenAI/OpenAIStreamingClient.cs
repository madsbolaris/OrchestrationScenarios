using Microsoft.Extensions.AI;
using OpenAI.Responses;
using AgentsSdk.Conversion;
using AgentsSdk.Models.Agents;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using AgentsSdk.Models.Tools.ToolDefinitions.Function;
using Microsoft.Extensions.Options;
using System.ClientModel;
using OpenAI;
using AgentsSdk.Models.Settings;
using System.Runtime.CompilerServices;

namespace AgentsSdk.Runtime.Streaming.Providers.OpenAI;

public sealed class OpenAIStreamingClient(IOptions<OpenAISettings> settings) : IStreamingAgentClient
{
    public async IAsyncEnumerable<StreamingUpdate> RunStreamingAsync(
        Agent agent,
        List<Models.Messages.ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {

        var responseItems = messages.SelectMany(ToResponseConverter.Convert).ToList();
        var options = new ResponseCreationOptions { StoredOutputEnabled = false };

        Dictionary<string, AIFunction> aiFunctions = [];

        if (agent.Tools != null)
        {
            foreach (var tool in agent.Tools)
            {
                options.Tools.Add(ToResponseConverter.Convert(tool));

                if (tool is FunctionToolDefinition fnTool)
                {
                    aiFunctions[fnTool.Name] = ToMicrosoftExtensionsAIContentConverter.ToAIFunction(fnTool);
                }
            }
        }

        var client = new OpenAIResponseClient(
            model: agent.Model.Id,
            credential: new ApiKeyCredential(settings.Value.ApiKey),
            options: new OpenAIClientOptions()
        );

        var response = client.CreateResponseStreamingAsync(responseItems, options, cancellationToken: cancellationToken);
        var conversationId = messages.FirstOrDefault()?.ConversationId ?? Guid.NewGuid().ToString();

        await foreach (var update in OpenAIStreamingProcessor.ProcessAsync(
            response,
            conversationId,
            async fn => await FunctionCallHelpers.ExecuteAsync(fn, aiFunctions),
            fn => FunctionCallHelpers.ResolveName(fn, agent.Tools),
            messages))
        {
            yield return update;
        }
    }
}
