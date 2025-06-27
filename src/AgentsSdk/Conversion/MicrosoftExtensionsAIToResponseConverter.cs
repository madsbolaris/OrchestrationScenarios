namespace AgentsSdk.Conversion;

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

internal static class MicrosoftExtensionsAIToResponseConverter
{
    public static ResponseTool Convert(AIFunction function, JsonNode parameters) => ConvertFunction(function, parameters);

    public static IEnumerable<ResponseItem> Convert(ChatMessage message) => ConvertChatMessage(message);

    private static ResponseTool ConvertFunction(AIFunction function, JsonNode parameters)
    {
        return ResponseTool.CreateFunctionTool(
            function.Name,
            function.Description,
            BinaryData.FromString(parameters.ToJsonString())
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
