using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowCreator.Models
{
    public class SchemaDefinition
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        [JsonPropertyName("properties")]
        public Dictionary<string, SchemaProperty>? Properties { get; set; }

        public SchemaDefinition Clone()
        {
            return new SchemaDefinition
            {
                Type = this.Type,
                Properties = this.Properties?.ToDictionary(
                    entry => entry.Key,
                    entry => new SchemaProperty
                    {
                        Type = entry.Value.Type,
                        Description = entry.Value.Description
                    }
                ) ?? new Dictionary<string, SchemaProperty>()
                {
                    { "PARAMETER_NAME", new SchemaProperty() }
                }
            };
        }

        public class SchemaProperty
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }
            [JsonPropertyName("required")]
            public bool Required { get; set; } = false;

            [JsonPropertyName("dynamicValues")]
            public JsonElement? DynamicValues { get; set; } = null;
        }


    }
}
