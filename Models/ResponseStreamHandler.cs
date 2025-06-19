namespace OrchestrationScenarios.Models;

using Microsoft.SemanticKernel;
using OpenAI.Responses;
using OrchestrationScenarios.Models.ContentParts;
using OrchestrationScenarios.Models.Helpers;

public sealed class ResponseStreamHandler
{
    private readonly OpenAIResponseClient _client;
    private readonly Kernel _kernel;

    public ResponseStreamHandler(OpenAIResponseClient client, Kernel kernel)
    {
        _client = client;
        _kernel = kernel;
    }

    public async IAsyncEnumerable<StreamedContentPart> RunStreamingAsync(List<Message> messages, List<ResponseTool> tools)
    {
        var chatMessages = messages.Select(ToKernelMessageConverter.Convert).ToList();
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

        var functionCallBuilders = new Dictionary<string, ContentParts.FunctionCallContent>();

        await foreach (var streamedPart in FromOpenAIResponsesStreamingResponseParser.ParseAsync(
            response,
            messages,
            functionCallBuilders,
            async fn => await FunctionCallExecutor.ExecuteAsync(fn, _kernel)
        ))
        {
            yield return streamedPart;
        }

        if (functionCallBuilders.Count > 0)
        {
            foreach (var part in HandleFunctionCalls(functionCallBuilders, messages))
                yield return part;
        }
    }

    private IEnumerable<StreamedContentPart> HandleFunctionCalls(
        Dictionary<string, ContentParts.FunctionCallContent> calls,
        List<Message> messages)
    {
        foreach (var functionCallContent in calls.Values)
        {
            yield return new StreamedStartContent(AuthorRole.Tool, functionCallContent.CallId!);

            var parts = functionCallContent.Name!.Split('-');
            if (parts.Length != 2)
            {
                throw new InvalidOperationException($"Expected function name in format 'plugin-function', but got '{functionCallContent.Name}'.");
            }

            var pluginName = parts[0];
            var functionName = parts[1];

            messages.Add(new Message
            {
                Role = AuthorRole.Tool,
                Content = [
                    new ContentParts.FunctionResultContent(
                        pluginName: pluginName,
                        functionName: functionName,
                        callId: functionCallContent.CallId!,
                        functionResult: functionCallContent.Results?.ToString() ?? string.Empty
                    )
                ]
            });

            yield return new StreamedFunctionResultContent(functionCallContent.CallId!, functionCallContent.CallId!, 0, functionCallContent.Results?.ToString() ?? string.Empty);
            yield return new StreamedEndContent(AuthorRole.Tool, functionCallContent.CallId!, 1);
        }
    }
}
