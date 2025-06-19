namespace OrchestrationScenarios.Models.Helpers;

using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using OrchestrationScenarios.Models.ContentParts;

public static class ToKernelMessageConverter
{
    public static ChatMessageContent Convert(Message message)
    {
        var items = new ChatMessageContentItemCollection();

        foreach (var contentPart in message.Content)
        {
            items.Add(contentPart switch
            {
                ContentParts.TextContent text => new Microsoft.SemanticKernel.TextContent(text.Text),
                ContentParts.FunctionCallContent call => new Microsoft.SemanticKernel.FunctionCallContent(
                    functionName: call.FunctionName,
                    pluginName: call.PluginName,
                    id: call.CallId,
                    arguments: new KernelArguments(JsonSerializer.Deserialize<Dictionary<string, object>>(call.Arguments)!)
                ),
                ContentParts.FunctionResultContent result => new Microsoft.SemanticKernel.FunctionResultContent(
                    result.FunctionName, result.PluginName, result.CallId, result.FunctionResult
                ),
                _ => throw new ArgumentException($"Unknown content part: {contentPart.GetType().Name}")
            });
        }

        return new ChatMessageContent(
            role: message.Role switch
            {
                Models.AuthorRole.User => AuthorRole.User,
                Models.AuthorRole.Agent => AuthorRole.Assistant,
                Models.AuthorRole.Tool => AuthorRole.Tool,
                Models.AuthorRole.Developer => AuthorRole.Developer,
                Models.AuthorRole.System => AuthorRole.System,
                _ => throw new ArgumentOutOfRangeException(nameof(message.Role), $"Unknown role: {message.Role}")
            },
            items: items
        );
    }
}
