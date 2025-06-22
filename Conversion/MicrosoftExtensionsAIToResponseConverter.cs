// File: Conversion/MicrosoftExtensionsAIToResponseConverter.cs
// Namespace: OrchestrationScenarios.Conversion

namespace OrchestrationScenarios.Conversion;

using System.Text.Json;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

public static class MicrosoftExtensionsAIToResponseConverter
{
    public static ResponseTool Convert(AIFunction function) => ConvertFunction(function);

    public static IEnumerable<ResponseItem> Convert(ChatMessage message) => ConvertChatMessage(message);

    private static ResponseTool ConvertFunction(AIFunction function)
    {
        if (!function.JsonSchema.TryGetProperty("properties", out var props))
            throw new InvalidOperationException("Missing 'properties' in JSON schema");

        return ResponseTool.CreateFunctionTool(
            function.Name,
            function.Description,
            BinaryData.FromString(props.GetRawText())
        );
    }

    private static IEnumerable<ResponseItem> ConvertChatMessage(ChatMessage message)
    {
        if (message.Contents.Count == 0)
            throw new InvalidOperationException("Chat message must have at least one content item.");

        var role = message.Role.ToString();
        var result = new List<ResponseItem>();

        foreach (var content in message.Contents)
        {
            result.Add(ConvertContentPart(role, message, content));
        }

        return result;
    }

    private static ResponseItem ConvertContentPart(string role, ChatMessage parent, AIContent content)
    {
        return (role, content) switch
        {
            ("system", TextContent text) => ResponseItem.CreateSystemMessageItem(text.Text),
            ("developer", TextContent text) => ResponseItem.CreateDeveloperMessageItem(text.Text),
            ("user", TextContent text) => ResponseItem.CreateUserMessageItem(text.Text),
            ("assistant", TextContent text) => ResponseItem.CreateAssistantMessageItem(text.Text),
            ("assistant", FunctionCallContent functionCall) =>
                ResponseItem.CreateFunctionCallItem(
                    functionCall.CallId,
                    functionCall.Name,
                    BinaryData.FromString(JsonSerializer.Serialize(functionCall.Arguments))
                ),
            ("tool", FunctionResultContent result) =>
                ResponseItem.CreateFunctionCallOutputItem(
                    result.CallId,
                    JsonSerializer.Serialize(result.Result)
                ),
            _ => throw new InvalidOperationException(
                $"Unsupported content type '{content.GetType().Name}' for role '{role}' in message '{parent.GetType().Name}'"
            )
        };
    }
}
