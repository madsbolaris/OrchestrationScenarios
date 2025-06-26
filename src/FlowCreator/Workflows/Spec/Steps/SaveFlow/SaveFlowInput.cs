
using System.ComponentModel;
using System.Text.Json.Serialization;
using FlowCreator.Models;

namespace FlowCreator.Workflows.Spec.Steps.CreateAction;

public class SaveFlowInput
{
    [JsonPropertyName("documentId")]
    public required Guid DocumentId { get; set; }
}
