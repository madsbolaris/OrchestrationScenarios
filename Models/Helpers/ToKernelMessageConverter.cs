namespace OrchestrationScenarios.Models.Helpers;

using System.Text.Json;
using Microsoft.Extensions.AI;
using TextContent = OrchestrationScenarios.Models.Messages.Content.TextContent;
using ToolCallContent = OrchestrationScenarios.Models.Messages.Content.ToolCallContent;
using ToolResultContent = OrchestrationScenarios.Models.Messages.Content.ToolResultContent;
using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Messages;

public static class ToMicrosoftExtensionsAIMessageConverter
{
    public static Microsoft.Extensions.AI.ChatMessage Convert(Messages.ChatMessage message)
    {
        var items = new List<AIContent>();

        foreach (var contentPart in message.Content)
        {
            items.Add(contentPart switch
            {
                TextContent text => new Microsoft.Extensions.AI.TextContent(text.Text),
                ToolCallContent call => new Microsoft.Extensions.AI.FunctionCallContent(
                    name: call.Name,
                    callId: call.ToolCallId,
                    arguments: call.Arguments
                ),
                ToolResultContent result when message is ToolMessage toolResultMessage => 
                    new Microsoft.Extensions.AI.FunctionResultContent(
                       toolResultMessage.ToolCallId, result.Results
                    ),
                ToolResultContent => throw new ArgumentException("Message must be of type ToolMessage for ToolResultContent."),
                _ => throw new ArgumentException($"Unknown content part: {contentPart.GetType().Name}")
            });
        }

        return new Microsoft.Extensions.AI.ChatMessage(
            role: message switch
            {
                UserMessage => ChatRole.User,
                AgentMessage => ChatRole.Assistant,
                ToolMessage => ChatRole.Tool,
                DeveloperMessage => new ChatRole("developer"),
                SystemMessage => ChatRole.System,
                _ => throw new ArgumentException($"Unknown message type: {message.GetType().Name}")
            },
            contents: items
        );
    }
}
