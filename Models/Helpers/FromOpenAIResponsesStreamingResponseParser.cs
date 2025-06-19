namespace OrchestrationScenarios.Models.Helpers;

using System.Text;
using OpenAI.Responses;
using OrchestrationScenarios.Models.ContentParts;

public static class FromOpenAIResponsesStreamingResponseParser
{
    public static async IAsyncEnumerable<StreamedContentPart> ParseAsync(
        IAsyncEnumerable<StreamingResponseUpdate> responseStream,
        List<Message> messages,
        Dictionary<string, FunctionCallContent> functionCallBuilders,
        Func<FunctionCallContent, Task<object?>> invokeFunction)
    {
        var responseBuilder = new StringBuilder();

        await foreach (var update in responseStream)
        {
            switch (update)
            {
                case StreamingResponseCreatedUpdate:
                case StreamingResponseInProgressUpdate:
                case StreamingResponseCompletedUpdate:
                case StreamingResponseTextAnnotationAddedUpdate:
                    break;

                case StreamingResponseOutputItemAddedUpdate outputItem:
                    if (outputItem.Item is FunctionCallResponseItem fnCall)
                    {
                        var fnContent = new FunctionCallContent(
                            callId: fnCall.CallId,
                            name: fnCall.FunctionName,
                            functionCallIndex: outputItem.OutputIndex
                        );
                        functionCallBuilders[fnCall.Id] = fnContent;

                        yield return new StreamedStartContent(AuthorRole.Agent, outputItem.Item.Id);
                        yield return new StreamedFunctionCallContent(messageId: outputItem.Item.Id, callId: fnCall.Id, index: 0, name: fnCall.FunctionName);
                    }
                    break;

                case StreamingResponseContentPartAddedUpdate contentPart:
                    responseBuilder.Clear();
                    yield return new StreamedStartContent(AuthorRole.Agent, contentPart.ItemId);
                    break;

                case StreamingResponseOutputTextDeltaUpdate textDelta:
                    responseBuilder.Append(textDelta.Delta);
                    yield return new StreamedTextContent(AuthorRole.Agent, textDelta.ItemId, textDelta.ContentIndex, textDelta.Delta);
                    break;

                case StreamingResponseOutputTextDoneUpdate textDone:
                    messages.Add(new Message
                    {
                        Role = AuthorRole.Agent,
                        Content = [new TextContent(responseBuilder.ToString())]
                    });
                    yield return new StreamedEndContent(AuthorRole.Agent, textDone.ItemId, textDone.ContentIndex + 1);
                    break;

                case StreamingResponseContentPartDoneUpdate partDone:
                    break;

                case StreamingResponseFunctionCallArgumentsDeltaUpdate fnArgsDelta:
                    yield return new StreamedFunctionCallContent(messageId: fnArgsDelta.ItemId, index: fnArgsDelta.OutputIndex, argumentDelta: fnArgsDelta.Delta);
                    if (functionCallBuilders.TryGetValue(fnArgsDelta.ItemId, out var fnContentDelta))
                    {
                        fnContentDelta.Arguments += fnArgsDelta.Delta;
                    }
                    break;

                case StreamingResponseFunctionCallArgumentsDoneUpdate fnArgsDone:
                    if (functionCallBuilders.TryGetValue(fnArgsDone.ItemId, out var doneFnContent))
                    {
                        messages.Add(new Message
                        {
                            Role = AuthorRole.Agent,
                            Content = [
                                new FunctionCallContent(
                                    callId: doneFnContent.CallId!,
                                    pluginName: doneFnContent.Name!.Split('-')[0],
                                    functionName: doneFnContent.Name!.Split('-')[1],
                                    arguments: doneFnContent.Arguments
                                )
                            ]
                        });

                        doneFnContent.Results = await invokeFunction(doneFnContent);

                        yield return new StreamedEndContent(AuthorRole.Agent, fnArgsDone.ItemId, fnArgsDone.OutputIndex + 1);
                    }
                    break;

                case StreamingResponseWebSearchCallInProgressUpdate webStart:
                    yield return new StreamedStartContent(AuthorRole.Agent, webStart.ItemId);
                    yield return new StreamedTextContent(AuthorRole.Agent, webStart.ItemId, 0, "WebSearch.Search()");
                    yield return new StreamedEndContent(AuthorRole.Agent, webStart.ItemId, 1);
                    yield return new StreamedStartContent(AuthorRole.Tool, "tool-" + webStart.ItemId);
                    break;

                case StreamingResponseWebSearchCallCompletedUpdate webEnd:
                    yield return new StreamedTextContent(AuthorRole.Tool, "tool-" + webEnd.ItemId, 0, "REDACTED");
                    yield return new StreamedEndContent(AuthorRole.Tool, "tool-" + webEnd.ItemId, 1);
                    break;

                case StreamingResponseOutputItemDoneUpdate:
                case StreamingResponseWebSearchCallSearchingUpdate:
                    break;

                default:
                    throw new InvalidOperationException($"Unknown response type: {update.GetType().Name}");
            }
        }
    }
}
