using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowCreator.Models;

[JsonConverter(typeof(AIDocumentConverter))]
public class AIDocument
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("version")]
    public int Version { get; set; } = 0;

    [JsonPropertyName("connectionReferenceLogicalName")]
    public string? ConnectionReferenceLogicalName { get; set; }

    [JsonPropertyName("inputSchema")]
    public SchemaDefinition InputSchema { get; set; } = new();

    [JsonPropertyName("actionSchema")]
    public FlowAction ActionSchema { get; set; } = new();

    // Helper properties for accessing ActionSchema.Inputs.Host values
    [JsonIgnore]
    public string? ApiName
    {
        get => ActionSchema?.Inputs?.Host?.ConnectionName;
        set
        {
            if (ActionSchema?.Inputs?.Host != null)
                ActionSchema.Inputs.Host.ConnectionName = value;
        }
    }

    [JsonIgnore]
    public string? OperationId
    {
        get => ActionSchema?.Inputs?.Host?.OperationId;
        set
        {
            if (ActionSchema?.Inputs?.Host != null)
                ActionSchema.Inputs.Host.OperationId = value;
        }
    }

    [JsonIgnore]
    public string? ApiId
    {
        get => ActionSchema?.Inputs?.Host?.ApiId;
        set
        {
            if (ActionSchema?.Inputs?.Host != null)
                ActionSchema.Inputs.Host.ApiId = value;
        }
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GetJsonSerializerOptions());
    }

    private static JsonSerializerOptions? jsonSerializerOptions = null;

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        if (jsonSerializerOptions != null)
            return jsonSerializerOptions;

        jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return jsonSerializerOptions;
    }
}


public class AIDocumentConverter : JsonConverter<AIDocument>
{
    public override AIDocument? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Optional: implement deserialization if needed
        throw new NotSupportedException("Deserialization is not supported for AIDocument.");
    }

    public override void Write(Utf8JsonWriter writer, AIDocument value, JsonSerializerOptions options)
    {
        var fullDefinition = new
        {
            schemaVersion = "1.0.0.0",
            properties = new
            {
                connectionReferences = new Dictionary<string, object>
                {
                    [value.ApiName ?? "CONNECTION_NAME"] = new
                    {
                        runtimeSource = "embedded",
                        connection = new
                        {
                            connectionReferenceLogicalName = value.ConnectionReferenceLogicalName ?? "CONNECTION_REFERENCE_NAME"
                        },
                        api = new
                        {
                            name = value.ApiName ?? "CONNECTION_NAME"
                        }
                    }
                },
                definition = new Dictionary<string, object>
                {
                    ["$schema"] = "https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#",
                    ["contentVersion"] = "1.0.0.0",
                    ["parameters"] = new Dictionary<string, object>
                    {
                        ["$connections"] = new { defaultValue = new { }, type = "Object" },
                        ["$authentication"] = new { defaultValue = new { }, type = "SecureObject" }
                    },
                    ["triggers"] = new
                    {
                        manual = new
                        {
                            type = "Request",
                            kind = "Http",
                            inputs = new
                            {
                                method = "POST",
                                schema = AddSchemaDefaults(value.InputSchema),
                                triggerAuthenticationType = "All"
                            }
                        }
                    },
                    ["actions"] = new
                    {
                        Try = new
                        {
                            type = "Scope",
                            actions = new
                            {
                                Action = AddActionDefaults(value.ActionSchema)
                            }
                        },
                        SuccessResponse = new
                        {
                            type = "Response",
                            kind = "Http",
                            inputs = new
                            {
                                statusCode = 200,
                                headers = new { Content_Type = "application/json" },
                                body = "@outputs('Action')?['body']"
                            },
                            runAfter = new { Try = new[] { "Succeeded" } }
                        },
                        Catch = new
                        {
                            type = "Scope",
                            actions = new
                            {
                                ErrorResponse = new
                                {
                                    type = "Response",
                                    kind = "Http",
                                    inputs = new
                                    {
                                        statusCode = 200,
                                        headers = new { Content_Type = "application/json" },
                                        body = "@outputs('Action')?['body']"
                                    }
                                }
                            },
                            runAfter = new { Try = new[] { "Failed" } }
                        }
                    }
                }
            }
        };

        JsonSerializer.Serialize(writer, fullDefinition, options);
    }

    private static FlowAction AddActionDefaults(FlowAction action)
    {
        var clone = action.Clone();

        clone.Inputs.Host.ConnectionName ??= "CONNECTION_NAME";
        clone.Inputs.Host.OperationId ??= "OPERATION_ID";
        clone.Inputs.Host.ApiId ??= "API_ID";

        if (clone.Inputs.Parameters == null || clone.Inputs.Parameters.Count == 0)
        {
            clone.Inputs.Parameters = new Dictionary<string, object>
            {
                { "PARAMETER_NAME", "@triggerBody()?['PARAMETER_NAME']" }
            };
        }

        return clone;
    }


    private static SchemaDefinition AddSchemaDefaults(SchemaDefinition schema)
    {
        if (schema.Properties == null || schema.Properties.Count == 0)
        {
            schema.Properties = new Dictionary<string, SchemaDefinition.SchemaProperty>
            {
                { "PARAMETER_NAME", new SchemaDefinition.SchemaProperty
                    {
                        Type = "PROPERTY_TYPE",
                        Description = "PROPERTY_DESCRIPTION"
                    }
                }
            };
        }

        foreach (var prop in schema.Properties.Values)
        {
            prop.Type ??= "PROPERTY_TYPE";
            prop.Description ??= "PROPERTY_DESCRIPTION";
        }

        return schema;
    }

}