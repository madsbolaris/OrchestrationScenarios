
using System.ComponentModel;
using System.Text.Json.Serialization;
using FlowCreator.Models;

namespace FlowCreator.Workflows.Spec.Steps.LoadExistingFlow;

public class LoadExistingFlowInput
{
    [JsonPropertyName("documentId")]
    public required Guid DocumentId { get; set; }
}
