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
    public static string SerializeStartTag(Type? type, Dictionary<string, string>? attributes = null)
    {
        if (type == null) return "";

        var tag = GetTag(type);
        var sb = new StringBuilder();

        using (var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = false,
            NewLineHandling = NewLineHandling.None
        }))
        {
            writer.WriteStartElement(tag);
            if (attributes != null)
            {
                foreach (var (key, value) in attributes)
                    writer.WriteAttributeString(key, value);
            }
            writer.WriteString(""); // ✅ Prevents <tag />
            writer.Flush();
        }

        // Remove trailing </tag>
        var xml = sb.ToString();
        var endTag = $"</{tag}>";
        if (xml.EndsWith(endTag))
            xml = xml[..^endTag.Length];

        return xml;
    }


    public static string SerializeEndTag(Type? type)
    {
        if (type == null) return "";
        return $"</{GetTag(type)}>\n"; // only one line break to avoid double spacing
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
        Escape(text ?? "");

    public static string Escape(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var sb = new StringBuilder();
        using var writer = CreateWriter(sb);
        writer.WriteStartElement("x");
        writer.WriteString(text);
        writer.WriteEndElement();
        writer.Flush();
        var result = sb.ToString();
        var start = result.IndexOf('>') + 1;
        var end = result.LastIndexOf('<');
        return result[start..end];
    }

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
}
