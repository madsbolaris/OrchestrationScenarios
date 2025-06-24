using System.Text.Json.Serialization;

namespace OrchestrationScenarios.Models.Messages.Content;

/// <summary>
/// Represents an annotation that links a portion of message content to a specific tool result or an external reference.
/// </summary>
public class Annotation
{
    /// <summary>
    /// The ID of the tool call that produced the result being referenced by this annotation.
    /// Optional if the annotation references an external URL instead.
    /// </summary>
    public string? ToolCallId { get; set; }

    /// <summary>
    /// A JSONPath query into the result object of the tool call.
    /// This path should locate the specific value within the tool result relevant to this annotation.
    /// Optional if referencing an external URL.
    /// </summary>
    [JsonPropertyName("path")]
    public string? JsonPath { get; set; }

    /// <summary>
    /// A URL to an external resource that provides additional context or references the annotated content.
    /// Optional if referencing a tool call result.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// The start index of the text span in the message content that this annotation applies to.
    /// </summary>
    public int? Start { get; set; }

    /// <summary>
    /// The end index of the text span in the message content that this annotation applies to.
    /// </summary>
    public int? End { get; set; }
}
