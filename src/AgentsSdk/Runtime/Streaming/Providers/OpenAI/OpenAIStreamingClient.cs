using System.Runtime.CompilerServices;
using AgentsSdk.Conversion;
using AgentsSdk.Models.Agents;
using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using AgentsSdk.Models.Settings;
using AgentsSdk.Models.Tools;
using AgentsSdk.Models.Tools.ToolDefinitions.BingGrounding;
using AgentsSdk.Models.Tools.ToolDefinitions.Function;
using AgentsSdk.Models.Tools.ToolDefinitions.PowerPlatform;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;
using AgentsSdk.Models.Runs.Responses.StreamingOperations;

namespace AgentsSdk.Runtime.Streaming.Providers.OpenAI;

public sealed class OpenAIStreamingClient(IOptions<OpenAISettings> settings) : IStreamingAgentClient
{
    public async IAsyncEnumerable<StreamingUpdate> RunStreamingAsync(
        Agent agent,
        List<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var prepended = false;

        try
        {
            if (agent.Instructions is { Count: > 0 })
            {
                messages.InsertRange(0, agent.Instructions);
                prepended = true;
            }

            var done = false;

            while (!done)
            {
                var responseItems = messages.SelectMany(ToResponseConverter.Convert).ToList();
                var options = new ResponseCreationOptions { StoredOutputEnabled = false };

                Dictionary<string, ToolMetadata> toolMetadataMap = [];

                if (agent.Tools is { Count: > 0 })
                {
                    foreach (var tool in agent.Tools)
                    {
                        var metadata = tool switch
                        {
                            PowerPlatformToolDefinition pp => pp.ToToolMetadata(),
                            FunctionToolDefinition fn => fn.ToToolMetadata(),
                            BingGroundingToolDefinition bing => bing.ToToolMetadata(),
                            _ => throw new NotSupportedException($"Unsupported tool type: {tool.GetType().Name}")
                        };

                        toolMetadataMap[metadata.Name] = metadata;
                        options.Tools.Add(ToolConversion.ToResponseTool(metadata));
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
                    toolMetadataMap,
                    messages,
                    cancellationToken))
                {
                    yield return update;

                    if (update is RunUpdate { Delta: EndStreamingOperation<Models.Runs.Responses.Deltas.RunDelta> })
                    {
                        done = true;
                        break;
                    }
                }
            }
        }
        finally
        {
            if (prepended)
                messages.RemoveRange(0, agent.Instructions!.Count);
        }
    }
}
