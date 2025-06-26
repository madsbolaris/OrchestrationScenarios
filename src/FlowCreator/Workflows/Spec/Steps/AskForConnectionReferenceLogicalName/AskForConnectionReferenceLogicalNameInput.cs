
using System.ComponentModel;
using System.Text.Json.Serialization;
using FlowCreator.Models;

namespace FlowCreator.Workflows.Spec.Steps.AskForConnectionReferenceLogicalName;

public class AskForConnectionReferenceLogicalNameInput
{
    [JsonPropertyName("documentId")]
    public required Guid DocumentId { get; set; }

    [JsonPropertyName("ConnectionReferenceLogicalName")]
    public required string ConnectionReferenceLogicalName { get; set; }
}
