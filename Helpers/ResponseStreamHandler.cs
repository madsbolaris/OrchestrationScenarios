namespace OrchestrationScenarios.Helpers;

using System.Linq;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

public sealed class ResponseStreamHandler
{
    private readonly OpenAIResponseClient _client;

    public ResponseStreamHandler(OpenAIResponseClient client)
    {
        _client = client;
    }

    public async IAsyncEnumerable<StreamingUpdate> RunStreamingAsync(
        List<Models.Messages.ChatMessage> messages,
        List<ResponseTool> tools,
        Dictionary<string, AIFunction> aiFunctions)
    {
        var chatMessages = messages.Select(ToMicrosoftExtensionsAIMessageConverter.Convert).ToList();
        var responseItems = chatMessages.SelectMany(ToResponseItemConverter.Convert).ToList();

        var options = new ResponseCreationOptions
        {
            StoredOutputEnabled = true
        };

        foreach (var tool in tools)
        {
            options.Tools.Add(tool);
        }

        var response = _client.CreateResponseStreamingAsync(responseItems, options);
        var conversationId = messages.FirstOrDefault()?.ConversationId ?? Guid.NewGuid().ToString();

        await foreach (var update in FromOpenAIResponsesStreamingResponseParser.ParseAsync(
            response,
            conversationId,
            async fn => await FunctionCallExecutor.ExecuteAsync(fn, aiFunctions),
            messages))
        {
            yield return update;
        }
    }
}
