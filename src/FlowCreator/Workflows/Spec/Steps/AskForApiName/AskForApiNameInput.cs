
using System.ComponentModel;
using System.Text.Json.Serialization;
using FlowCreator.Models;

namespace FlowCreator.Workflows.Spec.Steps.AskForApiName;

public class AskForApiNameInput
{
    [JsonPropertyName("document")]
    public required Guid DocumentId { get; set; }

    [JsonPropertyName("apiName")]
    public required string ApiName { get; set; }
}
