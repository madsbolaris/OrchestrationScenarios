namespace OrchestrationScenarios.Helpers;

using System.Text.Json;
using OpenAI.Responses;
using Microsoft.Extensions.AI;

public static class ToResponseItemConverter
{
    public static IEnumerable<ResponseItem> Convert(ChatMessage message)
    {
        var items = new List<ResponseItem>();

        if (message.Contents.Count == 0)
            throw new InvalidOperationException("Assistant messages must have at least one item.");

        switch (message.Role.ToString())
        {
            
            case "system":
                foreach (var item in message.Contents)
                {
                    if (item is TextContent text)
                    {
                        items.Add(ResponseItem.CreateSystemMessageItem(text.Text));
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unknown item type: {item.GetType().Name}");
                    }
                }
                break;
            
            case "developer":
                foreach (var item in message.Contents)
                {
                    if (item is TextContent text)
                    {
                        items.Add(ResponseItem.CreateDeveloperMessageItem(text.Text));
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unknown item type: {item.GetType().Name}");
                    }
                }
                break;

            case "user":
                foreach (var item in message.Contents)
                {
                    if (item is TextContent text)
                    {
                        items.Add(ResponseItem.CreateUserMessageItem(text.Text));
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unknown item type: {item.GetType().Name}");
                    }
                }
                break;

            case "assistant":
                foreach (var item in message.Contents)
                {
                    switch (item)
                    {
                        case TextContent text:
                            items.Add(ResponseItem.CreateAssistantMessageItem(text.Text));
                            break;

                        case FunctionCallContent functionCall:
                            items.Add(ResponseItem.CreateFunctionCallItem(
                                functionCall.CallId,
                                functionCall.Name,
                                BinaryData.FromString(JsonSerializer.Serialize(functionCall.Arguments))
                            ));
                            break;

                        default:
                            throw new InvalidOperationException($"Unknown item type: {item.GetType().Name}");
                    }
                }
                break;

            case "tool":
                foreach (var item in message.Contents)
                {
                    if (item is not FunctionResultContent functionResult)
                        throw new InvalidOperationException($"Expected FunctionResultContent, got {item.GetType().Name}");

                    items.Add(ResponseItem.CreateFunctionCallOutputItem(
                        functionResult.CallId,
                        JsonSerializer.Serialize(functionResult.Result)
                    ));
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(message.Role), $"Unsupported role: {message.Role}");
        }

        return items;
    }
}
