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
            public Dictionary<string, object> Parameters { get; set; } = new();

            [JsonPropertyName("authentication")]
            public object? Authentication { get; set; }

            public FlowActionInputs Clone()
            {
                return new FlowActionInputs
                {
                    Host = this.Host.Clone(),
                    Parameters = new Dictionary<string, object>(this.Parameters),
                    Authentication = this.Authentication // assumed immutable or reused safely
                };
            }

            public class FlowHost
            {
                [JsonPropertyName("connectionName")]
                public string ConnectionName { get; set; } = string.Empty;

                [JsonPropertyName("operationId")]
                public string OperationId { get; set; } = string.Empty;

                [JsonPropertyName("apiId")]
                public string ApiId { get; set; } = string.Empty;

                public FlowHost Clone()
                {
                    return new FlowHost
                    {
                        ConnectionName = this.ConnectionName,
                        OperationId = this.OperationId,
                        ApiId = this.ApiId
                    };
                }
            }
        }
    }
}
