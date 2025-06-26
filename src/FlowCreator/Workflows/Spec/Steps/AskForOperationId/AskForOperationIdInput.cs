
using System.ComponentModel;
using System.Text.Json.Serialization;
using FlowCreator.Models;

namespace FlowCreator.Workflows.Spec.Steps.AskForOperationId;

public class AskForOperationIdInput
{
    [JsonPropertyName("documentId")]
    public required Guid DocumentId { get; set; }

    [JsonPropertyName("operationId")]
    public required string OperationId { get; set; }
}
