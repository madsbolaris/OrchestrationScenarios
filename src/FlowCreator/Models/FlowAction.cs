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

            private Dictionary<string, object>? _parameters;

            [JsonPropertyName("parameters")]
            public Dictionary<string, object> Parameters
            {
                get => _parameters ?? new Dictionary<string, object>
        {
            { "PARAMETER_NAME", "@triggerBody()?['PARAMETER_NAME']" }
        };
                set => _parameters = value;
            }

            [JsonPropertyName("authentication")]
            public object? Authentication { get; set; }

            public FlowActionInputs Clone()
            {
                return new FlowActionInputs
                {
                    Host = this.Host.Clone(),
                    Parameters = new Dictionary<string, object>(this.Parameters),
                    Authentication = this.Authentication
                };
            }

            public class FlowHost
            {
                private string? _connectionName;
                private string? _operationId;
                private string? _apiId;

                [JsonPropertyName("connectionName")]
                public string? ConnectionName
                {
                    get => _connectionName ?? "CONNECTION_NAME";
                    set => _connectionName = value;
                }

                [JsonPropertyName("operationId")]
                public string? OperationId
                {
                    get => _operationId ?? "OPERATION_ID";
                    set => _operationId = value;
                }

                [JsonPropertyName("apiId")]
                public string? ApiId
                {
                    get => _apiId ?? "API_ID";
                    set => _apiId = value;
                }

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
