using System.Text.Json.Nodes;
using AgentsSdk.Models.Tools.ToolDefinitions.Function;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace AgentsSdk.Models.Tools.ToolDefinitions.PowerPlatform;

public class PowerPlatformToolDefinition : FunctionToolDefinition
{
    private readonly string _type;
    public override string Type => _type;

    public PowerPlatformToolDefinition(string type)
    {
        _type = type;

        // Parse API name and operationId from the type
        var match = Regex.Match(type, @"^Microsoft\.PowerPlatform\.([^-]+)-(.+)$");
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid PowerPlatform tool type: {type}");
        }

        var apiName = match.Groups[1].Value;
        var operationId = match.Groups[2].Value;

        var flowFilePath = Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Resources", "Flows")),
            $"{type}.json");

        if (!File.Exists(flowFilePath))
        {
            throw new FileNotFoundException($"Flow file not found for tool type '{type}'", flowFilePath);
        }

        var json = File.ReadAllText(flowFilePath);
        var doc = JsonNode.Parse(json)!;

        // Extract metadata
        var properties = doc["properties"];
        Description = properties?["description"]?.ToString() ?? "No description provided.";
        Name = $"{apiName}-{operationId}";

        var schema = properties?["definition"]?["triggers"]?["manual"]?["inputs"]?["schema"] as JsonObject;
        if (schema is not null)
        {
            ExtractRequiredRecursively(schema);
            Parameters = schema;
        }

        var callbackUrl = doc["callbackUrl"]?.ToString();
        if (string.IsNullOrEmpty(callbackUrl))
        {
            throw new InvalidOperationException("Callback URL missing from flow file.");
        }

        // Create the method delegate
        Method = async (JsonNode input) =>
        {
            using var httpClient = new HttpClient();

            var json = input.ToJsonString();
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(callbackUrl, content);

            return await response.Content.ReadAsStringAsync();
        };
    }

    internal override ToolMetadata ToToolMetadata()
    {
        return new ToolMetadata
        {
            Name = Name,
            Type = Type,
            Description = Description,
            Parameters = Parameters,
            Executor = Method is not null
                ? async (inputDict) =>
                {
                    JsonNode inputNode = ToJsonNode(inputDict); // move helper here
                    var result = Method.DynamicInvoke(inputNode);
                    return result is Task taskResult ? await ConvertAsync(taskResult) : result;
                }
                : null

        };
    }

    private static JsonNode ToJsonNode(Dictionary<string, object?> dict)
    {
        var node = new JsonObject();
        foreach (var (key, value) in dict)
        {
            node[key] = JsonSerializer.SerializeToNode(value);
        }
        return node;
    }


    private static async Task<object?> ConvertAsync(Task task)
    {
        await task.ConfigureAwait(false);

        var resultProp = task.GetType().GetProperty("Result");
        return resultProp?.GetValue(task);
    }

    private static void ExtractRequiredRecursively(JsonObject schema)
    {
        if (schema.TryGetPropertyValue("properties", out var propsNode) && propsNode is JsonObject props)
        {
            var requiredArray = new JsonArray();

            foreach (var (key, propNode) in props)
            {
                if (propNode is not JsonObject prop) continue;

                // Remove and collect 'required: true'
                if (prop.TryGetPropertyValue("required", out var requiredFlag))
                {
                    if (requiredFlag?.GetValue<bool>() == true)
                    {
                        requiredArray.Add(key);
                    }

                    // Always remove 'required' whether true or false
                    prop.Remove("required");
                }

                // Recurse if nested object with properties
                if (prop.TryGetPropertyValue("type", out var typeNode) &&
                    typeNode?.GetValue<string>() == "object" &&
                    prop.TryGetPropertyValue("properties", out _))
                {
                    ExtractRequiredRecursively(prop);
                }
            }

            if (requiredArray.Count > 0)
            {
                schema["required"] = requiredArray;
            }
        }
    }
}
