using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowCreator.Models;

[JsonConverter(typeof(FlowDefinitionConverter))]
public class FlowDefinition
{
    [JsonPropertyName("summar")]
    public string? Summary { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

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


public class FlowDefinitionConverter : JsonConverter<FlowDefinition>
{
    public override FlowDefinition? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var result = new FlowDefinition();

        var props = root.GetProperty("properties");

        // Extract summary and description
        if (props.TryGetProperty("summary", out var summaryNode))
        {
            result.Summary = summaryNode.GetString();
        }
        if (props.TryGetProperty("description", out var descriptionNode))
        {
            result.Description = descriptionNode.GetString();
        }

        // Extract connection reference
        if (props.TryGetProperty("connectionReferences", out var connRefs) && connRefs.ValueKind == JsonValueKind.Object)
        {
            foreach (var connRef in connRefs.EnumerateObject())
            {
                var name = connRef.Name;
                var connObj = connRef.Value;

                var connRefName = connObj.GetProperty("connection")
                                        .GetProperty("connectionReferenceLogicalName")
                                        .GetString();
                var apiName = connObj.GetProperty("api")
                                    .GetProperty("name")
                                    .GetString();

                result.ApiName = (apiName != "CONNECTION_NAME") ? apiName : null;
                result.ConnectionReferenceLogicalName = (connRefName != "CONNECTION_REFERENCE_NAME") ? connRefName : null;
                break;
            }
        }

        // Extract definition
        if (props.TryGetProperty("definition", out var def))
        {
            // INPUT SCHEMA
            var triggerInputs = def.GetProperty("triggers")
                                .GetProperty("manual")
                                .GetProperty("inputs");

            if (triggerInputs.TryGetProperty("schema", out var schemaNode))
            {
                var inputSchema = new SchemaDefinition
                {
                    Type = "object",
                    Properties = []
                };

                if (schemaNode.TryGetProperty("properties", out var propsNode))
                {
                    foreach (var prop in propsNode.EnumerateObject())
                    {
                        var type = prop.Value.GetProperty("type").GetString();
                        var description = prop.Value.GetProperty("description").GetString();

                        // Ignore hardcoded defaults
                        if (type == "PROPERTY_TYPE" && description == "PROPERTY_DESCRIPTION")
                            continue;

                        inputSchema.Properties[prop.Name] = new SchemaDefinition.SchemaProperty
                        {
                            Type = type == "PROPERTY_TYPE" ? null : type,
                            Description = description == "PROPERTY_DESCRIPTION" ? null : description
                        };
                    }
                }

                result.InputSchema = inputSchema;
            }

            // ACTION SCHEMA
            var actionNode = def.GetProperty("actions")
                .GetProperty("try")
                .GetProperty("actions")
                .GetProperty("action");

            var hostNode = actionNode.GetProperty("inputs").GetProperty("host");
            var operationId = hostNode.GetProperty("operationId").GetString();
            var connectionName = hostNode.GetProperty("connectionName").GetString();
            var apiId = hostNode.GetProperty("apiId").GetString();

            var parametersNode = actionNode.GetProperty("inputs").GetProperty("parameters");

            var parameters = new Dictionary<string, object>();
            foreach (var param in parametersNode.EnumerateObject())
            {
                var expr = param.Value.GetString();
                if (expr == "@triggerBody()?['PARAMETER_NAME']")
                    continue;

                parameters[param.Name] = expr!;
            }

            result.ActionSchema = new FlowAction
            {
                Inputs = new FlowAction.FlowActionInputs
                {
                    Host = new FlowAction.FlowActionInputs.FlowHost
                    {
                        ConnectionName = connectionName != "CONNECTION_NAME" ? connectionName : null,
                        OperationId = operationId != "OPERATION_ID" ? operationId : null,
                        ApiId = apiId != "API_ID" ? apiId : null
                    },
                    Parameters = parameters.Count > 0 ? parameters : null,
                    Authentication = null
                }
            };

            // Mirror operationId back to top-level helper
            result.OperationId = result.ActionSchema.Inputs.Host.OperationId;
            result.ApiId = result.ActionSchema.Inputs.Host.ApiId;
            result.ApiName = result.ActionSchema.Inputs.Host.ConnectionName;
        }

        return result;
    }


    public override void Write(Utf8JsonWriter writer, FlowDefinition value, JsonSerializerOptions options)
    {
        var fullDefinition = new
        {
            schemaVersion = "1.0.0.0",
            properties = new
            {
                summary = value.Summary ?? "FLOW_SUMMARY",
                description = value.Description ?? "FLOW_DESCRIPTION",
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
                    ["actions"] = new Dictionary<string, object>
                    {
                        ["try"] = new
                        {
                            type = "Scope",
                            actions = new
                            {
                                action = AddActionDefaults(value.ActionSchema)
                            }
                        },
                        ["successResponse"] = new
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
                        ["catch"] = new
                        {
                            type = "Scope",
                            actions = new
                            {
                                errorResponse = new
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
                            runAfter = new Dictionary<string, object> { ["try"] = new[] { "Failed" } }
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

        if (clone.Inputs.Parameters == null)
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
        if (schema.Properties == null)
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