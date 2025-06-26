
using System.ComponentModel;
using System.Text.Json.Serialization;
using FlowCreator.Models;

namespace FlowCreator.Workflows.Spec.Steps.AskForApiId;

public class AskForApiIdInput
{
    [JsonPropertyName("document")]
    public required Guid DocumentId { get; set; }

    [JsonPropertyName("apiId")]
    public required string ApiId { get; set; }
}
