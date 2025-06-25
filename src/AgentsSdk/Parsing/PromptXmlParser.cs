using System.Xml.Linq;
using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;

namespace AgentsSdk.Parsing;

public static class PromptXmlParser
{
    public static List<ChatMessage> Parse(string xmlFragment)
    {
        var doc = XDocument.Parse($"<root>{xmlFragment}</root>");
        var messages = new List<ChatMessage>();

        foreach (var node in doc.Root!.Elements())
        {
            var content = ParseContentParts(node);

            ChatMessage msg = node.Name.LocalName switch
            {
                "user" => new UserMessage { Content = content },
                "agent" => new AgentMessage { Content = content },
                "tool" => new ToolMessage
                {
                    ToolCallId = node.Attribute("for")?.Value 
                        ?? throw new InvalidOperationException($"Missing 'for' attribute on <tool> element."),
                    Content = content
                },
                "system" => new SystemMessage { Content = content },
                "developer" => new DeveloperMessage { Content = content },
                _ => throw new InvalidOperationException($"Unknown message type: <{node.Name}>")
            };

            messages.Add(msg);
        }

        return messages;
    }

    private static List<AIContent> ParseContentParts(XElement messageNode)
    {
        var contentParts = new List<AIContent>();

        foreach (var child in messageNode.Elements())
        {
            switch (child.Name.LocalName)
            {
                case "text":
                    contentParts.Add(new TextContent { Text = child.Value.Trim() });
                    break;

                case "tool-call":
                case "function-call":
                    contentParts.Add(new ToolCallContent
                    {
                        Name = child.Attribute("name")?.Value 
                            ?? throw new InvalidOperationException("Missing 'name' attribute on <tool-call>."),
                        ToolCallId = child.Attribute("id")?.Value 
                            ?? throw new InvalidOperationException("Missing 'id' attribute on <tool-call>."),
                        Arguments = ParseArguments(child)
                    });
                    break;

                case "tool-result":
                case "function-result":
                    contentParts.Add(new ToolResultContent
                    {
                        Results = child.Value.Trim()
                    });
                    break;

                default:
                    throw new InvalidOperationException($"Unknown content tag: <{child.Name.LocalName}>");
            }
        }

        return contentParts;
    }

    private static Dictionary<string, object?>? ParseArguments(XElement toolCall)
    {
        var args = toolCall.Elements("arg")
            .ToDictionary(
                e => e.Attribute("name")?.Value 
                    ?? throw new InvalidOperationException("Missing 'name' attribute on <arg>."),
                e => (object?)e.Value.Trim());

        return args.Count > 0 ? args : null;
    }
}
