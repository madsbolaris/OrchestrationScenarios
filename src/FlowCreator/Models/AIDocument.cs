using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowCreator.Models;

public class AIDocument
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("version")]
    public int Version { get; set; } = 0;

    [JsonPropertyName("swaggerFile")]
    public string SwaggerFile { get; set; } = string.Empty;

    [JsonPropertyName("connectionReferenceLogicalName")]
    public string ConnectionReferenceLogicalName { get; set; } = string.Empty;

    [JsonPropertyName("inputSchema")]
    public SchemaDefinition InputSchema { get; set; } = new();

    [JsonPropertyName("actionSchema")]
    public FlowAction ActionSchema { get; set; } = new();

    // Helper properties for accessing ActionSchema.Inputs.Host values
    [JsonIgnore]
    public string ApiName
    {
        get => ActionSchema?.Inputs?.Host?.ConnectionName ?? string.Empty;
        set
        {
            if (ActionSchema?.Inputs?.Host != null)
                ActionSchema.Inputs.Host.ConnectionName = value;
        }
    }

    [JsonIgnore]
    public string OperationId
    {
        get => ActionSchema?.Inputs?.Host?.OperationId ?? string.Empty;
        set
        {
            if (ActionSchema?.Inputs?.Host != null)
                ActionSchema.Inputs.Host.OperationId = value;
        }
    }

    [JsonIgnore]
    public string ApiId
    {
        get => ActionSchema?.Inputs?.Host?.ApiId ?? string.Empty;
        set
        {
            if (ActionSchema?.Inputs?.Host != null)
                ActionSchema.Inputs.Host.ApiId = value;
        }
    }

    public override string ToString()
    {
        var fullDefinition = new
        {
            schemaVersion = "1.0.0.0",
            properties = new
            {
                connectionReferences = new Dictionary<string, object>
                {
                    [ApiName] = new
                    {
                        runtimeSource = "embedded",
                        connection = new
                        {
                            connectionReferenceLogicalName = ConnectionReferenceLogicalName
                        },
                        api = new
                        {
                            name = ApiName
                        }
                    }
                },
                definition = new
                {
                    @schema = "https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#",
                    contentVersion = "1.0.0.0",
                    parameters = new
                    {
                        @connections = new { defaultValue = new { }, type = "Object" },
                        @authentication = new { defaultValue = new { }, type = "SecureObject" }
                    },
                    triggers = new
                    {
                        manual = new
                        {
                            type = "Request",
                            kind = "Http",
                            inputs = new
                            {
                                method = "POST",
                                schema = InputSchema,
                                triggerAuthenticationType = "All"
                            }
                        }
                    },
                    actions = new
                    {
                        Try = new
                        {
                            type = "Scope",
                            actions = new
                            {
                                Action = ActionSchema
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
                            runAfter = new
                            {
                                Try = new[] { "Succeeded" }
                            }
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
                            runAfter = new
                            {
                                Try = new[] { "Failed" }
                            }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(fullDefinition, GetJsonSerializerOptions());
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
