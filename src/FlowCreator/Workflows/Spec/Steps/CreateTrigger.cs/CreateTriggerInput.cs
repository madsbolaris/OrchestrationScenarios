
using System.ComponentModel;
using System.Text.Json.Serialization;
using FlowCreator.Models;

namespace FlowCreator.Workflows.Spec.Steps.CreateTrigger;

public class CreateTriggerInput
{
    [JsonPropertyName("documentId")]
    public required Guid DocumentId { get; set; }
}
