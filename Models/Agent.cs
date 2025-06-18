namespace OrchestrationScenarios.Models;

using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI;
using OpenAI.Responses;
using OrchestrationScenarios.Models.ContentParts;

public abstract class Agent(OpenAIResponseClient client)
{
    
    public string Model { get; set; } = "gpt-4";
    public List<Message> Preamble { get; set; } = [];
    public List<Message> Conclusion { get; set; } = [];
    public List<Tool> Tools { get; set; } = [];

    public async IAsyncEnumerable<StreamedContentPart> StreamRunAsync(List<Message> messages)
    {
        // Define the agent
        var agent = new OpenAIResponseAgent(client)
        {
            Name = "ResponseAgent",
            Instructions = "Answer all queries in the user's preferred language.",
        };

        var chatMessages = messages.Select((m) =>
        {
            var contents = new ChatMessageContentItemCollection();
            foreach (var contentPart in m.Content)
            {
                switch (contentPart)
                {
                    case ContentParts.TextContent textContent:
                        contents.Add(new Microsoft.SemanticKernel.TextContent(textContent.Text));
                        break;
                    case ContentParts.FunctionCallContent functionCallContent:
                        contents.Add(new Microsoft.SemanticKernel.FunctionCallContent(
                            functionName: functionCallContent.FunctionName,
                            pluginName: functionCallContent.PluginName,
                            id: functionCallContent.CallId,
                            arguments: new KernelArguments(JsonSerializer.Deserialize<Dictionary<string, object>>(functionCallContent.Arguments)!)
                        ));
                        break;
                    case ContentParts.FunctionResultContent functionResultContent:
                        contents.Add(new Microsoft.SemanticKernel.FunctionResultContent(
                            functionName: functionResultContent.FunctionName,
                            pluginName: functionResultContent.PluginName,
                            callId: functionResultContent.CallId,
                            functionResultContent.FunctionResult
                        ));
                        break;
                    default:
                        throw new ArgumentException($"Unknown content type: {contentPart.GetType().Name}");
                }
            }

            return new ChatMessageContent(
                role: m.Role switch
                {
                    AuthorRole.User => Microsoft.SemanticKernel.ChatCompletion.AuthorRole.User,
                    AuthorRole.Agent => Microsoft.SemanticKernel.ChatCompletion.AuthorRole.Assistant,
                    AuthorRole.Tool => Microsoft.SemanticKernel.ChatCompletion.AuthorRole.Tool,
                    AuthorRole.Developer => Microsoft.SemanticKernel.ChatCompletion.AuthorRole.Developer,
                    AuthorRole.System => Microsoft.SemanticKernel.ChatCompletion.AuthorRole.System,
                    _ => throw new ArgumentOutOfRangeException($"Unknown role: {m.Role}")
                },
                items: contents
            );
        });

        // Create function
        KernelFunction kernelFunction = KernelFunctionFactory.CreateFromMethod(() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Now", "Returns the current time in the format yyyy-MM-dd HH:mm:ss");
        var responseFunction = kernelFunction.ToResponseTool("DateTime");

        Kernel kernel = new();
        kernel.Plugins.Add(KernelPluginFactory.CreateFromFunctions("DateTime", [kernelFunction]));

        var options = new ResponseCreationOptions
        {
            StoredOutputEnabled = true,
            Tools = { ResponseTool.CreateWebSearchTool(), responseFunction }
        };


        while (true)
        {
            List<ResponseItem> items = [];

            foreach (var m in chatMessages)
            {
                switch (m.Role.ToString())
                {
                    case "system":
                    case "developer":
                    case "user":
                        items.Add(m.ToResponseItem());
                        break;

                    
                    case "assistant":
                        if (m.Items.Count == 0)
                        {
                            throw new InvalidOperationException("Assistant messages must have at least one item.");
                        }

                        foreach (var item in m.Items)
                        {
                            switch (item)
                            {
                                case Microsoft.SemanticKernel.TextContent textContent:
                                    items.Add(ResponseItem.CreateAssistantMessageItem(textContent.Text));
                                    break;

                                case Microsoft.SemanticKernel.FunctionCallContent functionCallContent:
                                    items.Add(ResponseItem.CreateFunctionCallItem(
                                        functionCallContent.Id,
                                        functionCallContent.FunctionName,
                                        BinaryData.FromString(JsonSerializer.Serialize(functionCallContent.Arguments))
                                    ));
                                    break;

                                default:
                                    throw new InvalidOperationException($"Unknown item type: {item.GetType().Name}");
                            }
                        }
                        break;

                    case "tool":
                        if (m.Items.Count == 0)
                        {
                            throw new InvalidOperationException("Tool messages must have at least one item.");
                        }

                        foreach (var item in m.Items)
                        {
                            if (item is not Microsoft.SemanticKernel.FunctionResultContent functionCallContent)
                            {
                                throw new InvalidOperationException($"Expected FunctionCallContent, but got {item.GetType().Name}.");
                            }

                            items.Add(ResponseItem.CreateFunctionCallOutputItem(
                                functionCallContent.CallId,
                                JsonSerializer.Serialize(functionCallContent.Result)
                            ));
                        }
                        break;
                }
            }

            // tracing: what happens when the agent asks questions without foreshadowing a tool?
            // validation: 
            // thing to measure: number of actions with dynamic schema falls to zero

            // setup weekly meeting with Gary
            // add error cases to scenarios
            // add topic scenarios
            // review with scenarios with Gary

            var response = client.CreateResponseStreamingAsync(items, options);

            Dictionary<string, FunctionCallContent> functionCallBuilders = [];
            StringBuilder responseBuilder = new();

            await foreach (var item in response)
            {
                switch (item)
                {
                    case StreamingResponseCreatedUpdate createdUpdate:
                        break;

                    case StreamingResponseOutputItemAddedUpdate outputItemAddedUpdate:
                        if (outputItemAddedUpdate.Item is FunctionCallResponseItem functionCallResponseItem)
                        {
                            functionCallBuilders.Add(functionCallResponseItem.Id, new FunctionCallContent(
                                callId: functionCallResponseItem.CallId,
                                name: functionCallResponseItem.FunctionName,
                                functionCallIndex: outputItemAddedUpdate.OutputIndex
                            ));

                            yield return new StreamedStartContent(AuthorRole.Agent, outputItemAddedUpdate.Item.Id);
                            yield return new StreamedTextContent(AuthorRole.Agent, outputItemAddedUpdate.Item.Id, 0, functionCallResponseItem.FunctionName + "(");
                        }
                        break;

                    case StreamingResponseContentPartAddedUpdate contentPartAddedUpdate:
                        responseBuilder = new StringBuilder();
                        yield return new StreamedStartContent(AuthorRole.Agent, contentPartAddedUpdate.ItemId);
                        break;

                    case StreamingResponseOutputTextDeltaUpdate textDeltaUpdate:
                        responseBuilder.Append(textDeltaUpdate.Delta);
                        yield return new StreamedTextContent(AuthorRole.Agent, textDeltaUpdate.ItemId, textDeltaUpdate.ContentIndex, textDeltaUpdate.Delta);
                        break;

                    case StreamingResponseContentPartDoneUpdate contentPartAddedUpdate:
                        messages.Add(new Message()
                        {
                            Role = AuthorRole.Agent,
                            Content = [new ContentParts.TextContent(responseBuilder.ToString())]
                        });

                        yield return new StreamedEndContent(AuthorRole.Agent, contentPartAddedUpdate.ItemId, contentPartAddedUpdate.ContentIndex + 1);
                        break;

                    case StreamingResponseFunctionCallArgumentsDeltaUpdate functionCallArgumentsDeltaUpdate:
                        yield return new StreamedTextContent(AuthorRole.Agent, functionCallArgumentsDeltaUpdate.ItemId, functionCallArgumentsDeltaUpdate.OutputIndex, functionCallArgumentsDeltaUpdate.Delta);
                        if (functionCallBuilders.TryGetValue(functionCallArgumentsDeltaUpdate.ItemId, out FunctionCallContent? functionCallContent))
                        {
                            functionCallContent.Arguments.Append(functionCallArgumentsDeltaUpdate.Delta);
                        }
                        break;

                    case StreamingResponseFunctionCallArgumentsDoneUpdate functionCallDoneUpdate:
                        yield return new StreamedTextContent(AuthorRole.Agent, functionCallDoneUpdate.ItemId, 0, ")");
                        if (functionCallBuilders.TryGetValue(functionCallDoneUpdate.ItemId, out FunctionCallContent? doneFunctionCallContent))
                        {
                            StreamingFunctionCallUpdateContent functionCallAddedUpdate = new(
                                callId: doneFunctionCallContent.CallId,
                                name: doneFunctionCallContent.Name,
                                arguments: doneFunctionCallContent.Arguments.ToString(),
                                functionCallIndex: doneFunctionCallContent.FunctionCallIndex
                            );
                            FunctionCallContentBuilder functionCallContentBuilder = new();
                            functionCallContentBuilder.Append(new StreamingChatMessageContent(Microsoft.SemanticKernel.ChatCompletion.AuthorRole.Assistant, null)
                            {
                                Items = [functionCallAddedUpdate]
                            });

                            var parts = doneFunctionCallContent.Name!.Split('-');

                            string pluginName = parts[0];
                            string functionName = parts[1];

                            messages.Add(new Message()
                            {
                                Role = AuthorRole.Agent,
                                Content = [new ContentParts.FunctionCallContent(
                                    callId: doneFunctionCallContent.CallId!,
                                    pluginName: doneFunctionCallContent.Name!.Split('-')[0],
                                    functionName: doneFunctionCallContent.Name!,
                                    arguments: doneFunctionCallContent.Arguments.ToString()
                                )]
                            });

                            doneFunctionCallContent.Results = (await functionCallContentBuilder.Build()[0].InvokeAsync(kernel)).Result;
                        }
                        yield return new StreamedEndContent(AuthorRole.Agent, functionCallDoneUpdate.ItemId, functionCallDoneUpdate.OutputIndex + 1);

                        break;

                    case StreamingResponseWebSearchCallInProgressUpdate webSearchInProgressUpdate:
                        yield return new StreamedStartContent(AuthorRole.Agent, webSearchInProgressUpdate.ItemId);
                        yield return new StreamedTextContent(AuthorRole.Agent, webSearchInProgressUpdate.ItemId, 0, "WebSearch.Search()");
                        yield return new StreamedEndContent(AuthorRole.Agent, webSearchInProgressUpdate.ItemId, 1);

                        yield return new StreamedStartContent(AuthorRole.Tool, "tool-" + webSearchInProgressUpdate.ItemId);
                        break;

                    case StreamingResponseWebSearchCallSearchingUpdate webSearchSearchingUpdate:
                        break;

                    case StreamingResponseWebSearchCallCompletedUpdate webSearchCompletedUpdate:
                        yield return new StreamedTextContent(AuthorRole.Tool, "tool-" + webSearchCompletedUpdate.ItemId, 0, "REDACTED");
                        yield return new StreamedEndContent(AuthorRole.Tool, "tool-" + webSearchCompletedUpdate.ItemId, 1);
                        break;

                    case StreamingResponseOutputItemDoneUpdate doneUpdate:
                        break;
                }
            }

            if (functionCallBuilders.Count == 0)
            {
                break;
            }

            foreach (var functionCallContent in functionCallBuilders.Values)
            {
                yield return new StreamedStartContent(AuthorRole.Tool, functionCallContent.CallId!);
                var parts = functionCallContent.Name!.Split('-');
                if (parts.Length != 2)
                {
                    throw new InvalidOperationException($"Expected function name in format 'plugin-function', but got '{functionCallContent.Name}'.");
                }

                string pluginName = parts[0];
                string functionName = parts[1];


                messages.Add(new Message()
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
                yield return new StreamedTextContent(AuthorRole.Tool, functionCallContent.CallId!, 0, functionCallContent.Results?.ToString() ?? string.Empty);
                yield return new StreamedEndContent(AuthorRole.Tool, functionCallContent.CallId!, 1);
            }
        }
    }
}

internal class FunctionCallContent(string? callId, string? name, int functionCallIndex = 0)
{
    public string? CallId { get; set; } = callId;
    public string? Name { get; set; } = name;
    public StringBuilder Arguments { get; set; } = new();
    public int FunctionCallIndex { get; set; } = functionCallIndex;
    public object? Results { get; set; }
}