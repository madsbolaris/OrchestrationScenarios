
using System.ComponentModel;
using System.Text.Json.Serialization;
using FlowCreator.Models;

namespace FlowCreator.Workflows.Spec.Summary.Steps;

public class AskForApiIdInput
{
    [JsonPropertyName("apiId")]
    public required string ApiId { get; set; }

    [JsonPropertyName("document")]
    public required Guid DocumentId { get; set; }
}
