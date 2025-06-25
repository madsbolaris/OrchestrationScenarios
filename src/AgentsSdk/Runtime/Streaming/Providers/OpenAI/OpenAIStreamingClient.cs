using Microsoft.Extensions.AI;
using OpenAI.Responses;
using AgentsSdk.Conversion;
using AgentsSdk.Models.Agents;
using AgentsSdk.Models.Runs.Responses.StreamingUpdates;
using AgentsSdk.Models.Tools.ToolDefinitions.Function;

namespace AgentsSdk.Runtime.Streaming.Providers.OpenAI;

public sealed class OpenAIStreamingClient(OpenAIResponseClient client) : IStreamingAgentClient
{
    public async IAsyncEnumerable<StreamingUpdate> RunStreamingAsync(
        Agent agent,
        List<Models.Messages.ChatMessage> messages)
    {
        var responseItems = messages.SelectMany(ToResponseConverter.Convert).ToList();
        var options = new ResponseCreationOptions { StoredOutputEnabled = true };

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

        var response = client.CreateResponseStreamingAsync(responseItems, options);
        var conversationId = messages.FirstOrDefault()?.ConversationId ?? Guid.NewGuid().ToString();

        await foreach (var update in OpenAIStreamingProcessor.ProcessAsync(
            response,
            conversationId,
            async fn => await FunctionCallExecutor.ExecuteAsync(fn, aiFunctions),
            messages))
        {
            yield return update;
        }
    }
}
