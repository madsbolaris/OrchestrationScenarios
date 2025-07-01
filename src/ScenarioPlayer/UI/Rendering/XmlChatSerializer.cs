using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml;
using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Runs.Responses.Deltas;
using AgentsSdk.Models.Runs.Responses.Deltas.Content;

namespace ScenarioPlayer.UI.Rendering;

public static class XmlChatSerializer
{
    public static string SerializeStartTag(Type? type, string? attributes = null)
    {
        var tag = GetTag(type);
        var sb = new StringBuilder();
        using var writer = CreateWriter(sb);

        writer.WriteStartElement(tag);
        if (attributes != null)
        {
            // Manually write raw attribute string into tag
            writer.WriteRaw(" " + attributes);
        }

        writer.WriteString(""); // force tag to stay open
        writer.WriteEndElement(); // will reopen/close immediately
        writer.Flush();

        var xml = sb.ToString();
        // Trim self-closing tag, convert to open-only: <tag></tag> => <tag>
        return xml.Replace($"</{tag}>", "").TrimEnd() + "\n";
    }

    public static string SerializeEndTag(Type? type)
    {
        var tag = GetTag(type);
        return $"</{tag}>\n";
    }

    public static string SerializeContent(AIContent content)
    {
        return content switch
        {
            TextContent text => WrapElement("text", text.Text),
            ToolCallContent call => WrapElement("tool-call", call.Arguments?.ToString() ?? "", w =>
            {
                w.WriteAttributeString("name", call.Name);
                w.WriteAttributeString("id", call.ToolCallId);
            }),
            ToolResultContent result => WrapElement("tool-result", SerializeResultValue(result)),
            _ => ""
        };
    }

    public static string SerializeToolResult(ToolResultContent result)
        => WrapElement("tool-result", SerializeResultValue(result));

    public static string SerializeAppendValue(string? text) =>
        Escape(text ?? "") + "\n";

    private static string WrapElement(string tagName, string value, Action<XmlWriter>? writeAttributes = null)
    {
        var sb = new StringBuilder();
        using var writer = CreateWriter(sb);

        writer.WriteStartElement(tagName);
        writeAttributes?.Invoke(writer);
        writer.WriteString(value);
        writer.WriteEndElement();
        writer.Flush();

        return sb.ToString() + "\n";
    }

    private static XmlWriter CreateWriter(StringBuilder sb)
    {
        return XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = true,
            NewLineOnAttributes = false
        });
    }

    private static string SerializeResultValue(ToolResultContent result)
    {
        return result.Results switch
        {
            string s => s,
            _ => JsonSerializer.Serialize(result.Results, new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            })
        };
    }

    public static string GetTag(Type? type) => type switch
    {
        var t when t == typeof(ToolMessageDelta) || t == typeof(ToolMessage) => "tool",
        var t when t == typeof(AgentMessageDelta) || t == typeof(AgentMessage) => "agent",
        var t when t == typeof(UserMessage) => "user",
        var t when t == typeof(TextContentDelta) || t == typeof(TextContent) => "text",
        var t when t == typeof(ToolCallContentDelta) || t == typeof(ToolCallContent) => "tool-call",
        var t when t == typeof(ToolResultContent) => "tool-result",
        _ => "unknown"
    };

    public static string Escape(string text)
    {
        // Use XmlWriter’s escaping logic via surrogate wrap
        var sb = new StringBuilder();
        using var writer = CreateWriter(sb);
        writer.WriteStartElement("dummy");
        writer.WriteString(text);
        writer.WriteEndElement();
        writer.Flush();

        var result = sb.ToString();
        var start = result.IndexOf('>') + 1;
        var end = result.LastIndexOf('<');
        return result[start..end];
    }
}
