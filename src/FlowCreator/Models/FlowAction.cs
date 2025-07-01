using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlowCreator.Models
{
    public class FlowAction
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "OpenApiConnection";

        [JsonPropertyName("inputs")]
        public FlowActionInputs Inputs { get; set; } = new();

        public FlowAction Clone()
        {
            return new FlowAction
            {
                Type = this.Type,
                Inputs = this.Inputs.Clone()
            };
        }

        public class FlowActionInputs
        {
            [JsonPropertyName("host")]
            public FlowHost Host { get; set; } = new();

            [JsonPropertyName("parameters")]
            public Dictionary<string, object>? Parameters { get; set; }

            [JsonPropertyName("authentication")]
            public object? Authentication { get; set; }

            public FlowActionInputs Clone()
            {
                return new FlowActionInputs
                {
                    Host = this.Host.Clone(),
                    Parameters = this.Parameters != null
                        ? new Dictionary<string, object>(this.Parameters)
                        : null,
                    Authentication = this.Authentication
                };
            }

            public class FlowHost
            {
                [JsonPropertyName("connectionName")]
                public string? ConnectionName { get; set; }

                [JsonPropertyName("connectorName")]
                public string? ConnectorName { get; set; }

                [JsonPropertyName("apiName")]
                public string? ApiName { get; set; }

                [JsonPropertyName("operationId")]
                public string? OperationId { get; set; }

                [JsonPropertyName("apiId")]
                public string? ApiId { get; set; }

                public FlowHost Clone()
                {
                    return new FlowHost
                    {
                        ConnectionName = this.ConnectionName,
                        ConnectorName = this.ConnectorName,
                        OperationId = this.OperationId,
                        ApiName = this.ApiName,
                        ApiId = this.ApiId
                    };
                }
            }
        }

    }
}
