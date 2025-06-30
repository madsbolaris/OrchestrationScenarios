using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AgentsSdk.Helpers;
using AgentsSdk.Models.Tools.ToolDefinitions.Function;

namespace AgentsSdk.Models.Tools.ToolDefinitions.PowerPlatform;

public class PowerPlatformToolDefinition : FunctionToolDefinition
{
    private readonly string _type;
    public override string Type => _type;

    protected JsonNode? _baseParameters;
    private JsonNode? _mergedParameters;

    public override JsonNode? Parameters
    {
        get
        {
            _mergedParameters ??= SchemaMerger.Merge(_baseParameters, Overrides?.Parameters);
            return _mergedParameters;
        }
    }

    private ToolOverrides? _overrides;
    public override ToolOverrides? Overrides
    {
        get => _overrides;
        set
        {
            _overrides = value;
            _mergedParameters = null;
        }
    }

    private readonly List<ToolDefinition> _childTools = [];
    public IEnumerable<ToolDefinition> ChildTools => _childTools;

    public PowerPlatformToolDefinition(string type)
    {
        _type = type;

        // Parse API name and operationId from the type
        var match = Regex.Match(type, @"^Microsoft\.PowerPlatform\.([^-]+)-(.+)$");
        if (!match.Success)
            throw new ArgumentException($"Invalid PowerPlatform tool type: {type}");

        var apiName = match.Groups[1].Value;
        var operationId = match.Groups[2].Value;

        var flowFilePath = Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Resources", "Flows")),
            $"{type}.json");

        if (!File.Exists(flowFilePath))
            throw new FileNotFoundException($"Flow file not found for tool type '{type}'", flowFilePath);

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
            _baseParameters = schema;

            if (schema.TryGetPropertyValue("properties", out var propsNode) && propsNode is JsonObject props)
            {
                foreach (var (propName, propValue) in props)
                {
                    if (propValue is not JsonObject propObj)
                        continue;

                    if (propObj.TryGetPropertyValue("dynamicValues", out var dynamicNode) && dynamicNode is JsonObject dynamicValues)
                    {
                        var childTool = CreateDynamicListEnumTool(Name, propName, dynamicValues, props);
                        if (childTool is not null)
                            _childTools.Add(childTool);
                    }
                }
            }
        }

        var callbackUrl = doc["callbackUrl"]?.ToString();
        if (string.IsNullOrEmpty(callbackUrl))
            throw new InvalidOperationException("Callback URL missing from flow file.");

        // Create the method delegate
        Method = async (JsonNode input) =>
        {
            using var httpClient = new HttpClient();
            var content = new StringContent(input.ToJsonString(), System.Text.Encoding.UTF8, "application/json");
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
                    var effectiveSchema = Overrides?.Parameters ?? Parameters;
                    var normalized = ToolArgumentNormalizer.NormalizeArguments(effectiveSchema, inputDict);

                    var inputNode = new JsonObject();
                    foreach (var (key, value) in normalized)
                    {
                        inputNode[key] = JsonSerializer.SerializeToNode(value);
                    }

                    var result = Method.DynamicInvoke(inputNode);
                    return result is Task taskResult ? await ConvertAsync(taskResult) : result;
                }
            : null
        };
    }

    private static async Task<object?> ConvertAsync(Task task)
    {
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }

    private static void ExtractRequiredRecursively(JsonObject schema)
    {
        if (schema.TryGetPropertyValue("properties", out var propsNode) && propsNode is JsonObject props)
        {
            var requiredArray = new JsonArray();

            foreach (var (key, propNode) in props)
            {
                if (propNode is not JsonObject prop) continue;

                if (prop.TryGetPropertyValue("required", out var requiredFlag))
                {
                    if (requiredFlag?.GetValue<bool>() == true)
                        requiredArray.Add(key);

                    prop.Remove("required");
                }

                if (prop.TryGetPropertyValue("type", out var typeNode) &&
                    typeNode?.GetValue<string>() == "object" &&
                    prop.TryGetPropertyValue("properties", out _))
                {
                    ExtractRequiredRecursively(prop);
                }
            }

            if (requiredArray.Count > 0)
                schema["required"] = requiredArray;
        }
    }

    private static ToolDefinition? CreateDynamicListEnumTool(
        string parentToolName,
        string propName,
        JsonObject dynamicValues,
        JsonObject schemaProperties)
    {
        if (!dynamicValues.TryGetPropertyValue("operationId", out var opNode) || opNode is null)
            return null;

        var operationId = opNode.GetValue<string>();

        var parameters = dynamicValues["parameters"] is JsonObject original
            ? JsonSerializer.Deserialize<JsonObject>(original.ToJsonString())!
            : new JsonObject();

        var valueCollection = dynamicValues["value-collection"]?.GetValue<string>() ?? "value";
        var valuePath = dynamicValues["value-path"]?.GetValue<string>() ?? "id";
        var valueTitle = dynamicValues["value-title"]?.GetValue<string>() ?? "name";

        var parts = parentToolName.Split('-', 2, StringSplitOptions.RemoveEmptyEntries);
        var api = parts.ElementAtOrDefault(0) ?? "UnknownApi";
        var formattedProp = char.ToUpperInvariant(propName[0]) + propName[1..];

        var type = $"Microsoft.PowerPlatform.{api}.ListEnum-{formattedProp}";
        var name = $"ListEnum-{formattedProp}";

        var inputDescriptions = new Dictionary<string, string>();
        foreach (var (key, val) in parameters)
        {
            if (val is JsonObject paramObj &&
                paramObj.TryGetPropertyValue("parameter", out var refNode) &&
                refNode?.GetValue<string>() is string refName &&
                schemaProperties.TryGetPropertyValue(refName, out var refPropNode) &&
                refPropNode is JsonObject refPropObj &&
                refPropObj.TryGetPropertyValue("description", out var descNode))
            {
                inputDescriptions[refName] = descNode?.GetValue<string>() ?? refName;
            }
        }

        return new ListEnumToolDefinition(
            type,
            name,
            operationId,
            parameters,
            valueCollection,
            valuePath,
            valueTitle,
            inputDescriptions);
    }
}
