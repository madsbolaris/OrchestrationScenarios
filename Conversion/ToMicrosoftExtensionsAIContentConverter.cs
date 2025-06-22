namespace OrchestrationScenarios.Conversion;

using Microsoft.Extensions.AI;
using Models.Messages.Content;
using Models.Messages.Types;

public static class ToMicrosoftExtensionsAIContentConverter
{
    public static ChatMessage Convert(Models.Messages.ChatMessage message)
    {
        var contents = message.Content.Select(content => ConvertContent(message, content)).ToList();
        var role = ConvertRole(message);

        return new ChatMessage(role, contents);
    }

    private static Microsoft.Extensions.AI.AIContent ConvertContent(Models.Messages.ChatMessage parent, Models.Messages.Content.AIContent content)
    {
        return content switch
        {
            Models.Messages.Content.TextContent text => new Microsoft.Extensions.AI.TextContent(text.Text),
            ToolCallContent call => new FunctionCallContent(
                name: call.Name,
                callId: call.ToolCallId,
                arguments: call.Arguments
            ),
            ToolResultContent result when parent is ToolMessage toolMsg => new FunctionResultContent(
                toolMsg.ToolCallId,
                result.Results
            ),
            ToolResultContent => throw new ArgumentException("ToolResultContent must appear in a ToolMessage."),
            _ => throw new ArgumentException($"Unsupported content type: {content.GetType().Name}")
        };
    }

    private static ChatRole ConvertRole(Models.Messages.ChatMessage message)
    {
        return message switch
        {
            UserMessage => ChatRole.User,
            AgentMessage => ChatRole.Assistant,
            ToolMessage => ChatRole.Tool,
            DeveloperMessage => new ChatRole("developer"),
            SystemMessage => ChatRole.System,
            _ => throw new ArgumentException($"Unknown ChatMessage type: {message.GetType().Name}")
        };
    }
}
