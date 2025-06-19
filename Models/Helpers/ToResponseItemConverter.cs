namespace OrchestrationScenarios.Models.Helpers;

using System.Text.Json;
using OpenAI.Responses;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OrchestrationScenarios.Models.ContentParts;
using Microsoft.SemanticKernel.Agents.OpenAI;

public static class ToResponseItemConverter
{
    public static IEnumerable<ResponseItem> Convert(ChatMessageContent message)
    {
        var items = new List<ResponseItem>();

        switch (message.Role.ToString())
        {
            case "system":
            case "developer":
            case "user":
                items.Add(message.ToResponseItem());
                break;

            case "assistant":
                if (message.Items.Count == 0)
                    throw new InvalidOperationException("Assistant messages must have at least one item.");

                foreach (var item in message.Items)
                {
                    switch (item)
                    {
                        case Microsoft.SemanticKernel.TextContent text:
                            items.Add(ResponseItem.CreateAssistantMessageItem(text.Text));
                            break;

                        case Microsoft.SemanticKernel.FunctionCallContent functionCall:
                            items.Add(ResponseItem.CreateFunctionCallItem(
                                functionCall.Id,
                                functionCall.FunctionName,
                                BinaryData.FromString(JsonSerializer.Serialize(functionCall.Arguments))
                            ));
                            break;

                        default:
                            throw new InvalidOperationException($"Unknown item type: {item.GetType().Name}");
                    }
                }
                break;

            case "tool":
                if (message.Items.Count == 0)
                    throw new InvalidOperationException("Tool messages must have at least one item.");

                foreach (var item in message.Items)
                {
                    if (item is not Microsoft.SemanticKernel.FunctionResultContent functionResult)
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
