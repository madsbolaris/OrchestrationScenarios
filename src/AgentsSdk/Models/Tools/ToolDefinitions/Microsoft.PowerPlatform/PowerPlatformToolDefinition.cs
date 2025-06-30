using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AgentsSdk.Models.Tools.ToolDefinitions.PowerPlatform;

public class PowerPlatformToolDefinition : ClientSideToolDefinition
{
    private readonly string _type;
    public override string Type => _type;

    private readonly List<ToolDefinition> _childTools = [];
    public IEnumerable<ToolDefinition> ChildTools => _childTools;

    public Delegate? Method { get; private set; }

    public PowerPlatformToolDefinition(string type, ToolOverrides? overrides = null) : base(type)
    {
        _type = type;
        Overrides = overrides;

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

        var properties = doc["properties"];
        Description = properties?["description"]?.ToString() ?? "No description provided.";
        Name = $"{apiName}-{operationId}";

        var schema = properties?["definition"]?["triggers"]?["manual"]?["inputs"]?["schema"] as JsonObject;
        if (schema is not null)
        {
            ExtractRequiredRecursively(schema);
            _baseParameters = schema;

            if (Parameters is JsonObject merged &&
                merged.TryGetPropertyValue("properties", out var propsNode) &&
                propsNode is JsonObject props)
            {
                foreach (var (propName, propValue) in props)
                {
                    if (propValue is not JsonObject propObj ||
                        !propObj.TryGetPropertyValue("dynamicValues", out var dynamicNode) ||
                        dynamicNode is not JsonObject dynamicValues)
                        continue;

                    object? readOnlyExpectedValue = null;
                    if (propObj.TryGetPropertyValue("readonly", out var readonlyNode) &&
                        ToolArgumentNormalizer.IsJsonTrue(readonlyNode) &&
                        propObj.TryGetPropertyValue("default", out var defaultNode))
                    {
                        readOnlyExpectedValue = defaultNode?.Deserialize<object>();
                    }

                    var childTool = CreateDynamicListEnumTool(
                        Name,
                        propName,
                        dynamicValues,
                        Parameters,
                        readOnlyExpectedValue);

                    if (childTool is not null)
                        _childTools.Add(childTool);
                }
            }
        }

        var callbackUrl = doc["callbackUrl"]?.ToString();
        if (string.IsNullOrEmpty(callbackUrl))
            throw new InvalidOperationException("Callback URL missing from flow file.");

        Method = async (JsonNode input) =>
        {
            using var httpClient = new HttpClient();
            var content = new StringContent(input.ToJsonString(), System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(callbackUrl, content);
            return await response.Content.ReadAsStringAsync();
        };

        _executor = async (inputDict) =>
        {
            var effectiveSchema = Overrides?.Parameters ?? Parameters;
            var normalized = ToolArgumentNormalizer.NormalizeArguments(effectiveSchema, inputDict);
            var inputNode = new JsonObject();
            foreach (var (key, value) in normalized)
            {
                inputNode[key] = JsonSerializer.SerializeToNode(value);
            }

            var result = Method!.DynamicInvoke(inputNode);
            return result is Task taskResult ? await ConvertAsync(taskResult) : result;
        };
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
                    {
                        requiredArray.Add(key);
                    }
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
        JsonNode mergedParametersNode,
        object? readOnlyExpectedValue)
    {
        if (!dynamicValues.TryGetPropertyValue("operationId", out var opNode))
            return null;

        var operationId = opNode!.GetValue<string>();

        var paramRefs = new Dictionary<string, JsonObject>();

        if (dynamicValues.TryGetPropertyValue("parameters", out var parametersNode) &&
            parametersNode is JsonObject parametersObj)
        {
            foreach (var (key, valNode) in parametersObj)
            {
                if (valNode is JsonObject obj)
                {
                    paramRefs[key] = obj;
                }
                else
                {
                    // Wrap primitive as { "value": primitive } for uniform handling
                    paramRefs[key] = new JsonObject
                    {
                        ["value"] = JsonNode.Parse(valNode!.ToJsonString())!
                    };
                }
            }
        }

        var inputSchema = new JsonObject();
        var invocationParams = new Dictionary<string, ListEnumParameterReference>();

        if (mergedParametersNode is not JsonObject mergedParameters ||
            !mergedParameters.TryGetPropertyValue("properties", out var propsNode) ||
            propsNode is not JsonObject allProps)
            return null;

        foreach (var (inputName, meta) in paramRefs)
        {
            if (meta.TryGetPropertyValue("parameter", out var refNode))
            {
                var parameterName = refNode!.GetValue<string>();

                if (allProps.TryGetPropertyValue(parameterName, out var sourceProp) &&
                    sourceProp is JsonObject propObj)
                {
                    inputSchema[inputName] = JsonNode.Parse(propObj.ToJsonString())!;
                }

                invocationParams[inputName] = new ListEnumParameterReference
                {
                    ParameterName = parameterName
                };
            }
            else
            {
                invocationParams[inputName] = new ListEnumParameterReference
                {
                    StaticValue = meta["value"]
                };
            }
        }

        var parts = parentToolName.Split('-', 2);
        var api = parts.ElementAtOrDefault(0) ?? "UnknownApi";
        var formattedProp = char.ToUpperInvariant(propName[0]) + propName[1..];

        var type = $"Microsoft.PowerPlatform.{api}.ListEnum-{formattedProp}";
        var name = $"ListEnum-{formattedProp}";

        return new ListEnumToolDefinition(
            type,
            name,
            inputSchema,
            invocationParams,
            new ListEnumSettings
            {
                OperationId = operationId,
                ValueCollection = dynamicValues["value-collection"]?.GetValue<string>() ?? "value",
                ValuePath = dynamicValues["value-path"]?.GetValue<string>() ?? "id",
                ValueTitle = dynamicValues["value-title"]?.GetValue<string>() ?? "name",
                ReadOnlyExpectedValue = readOnlyExpectedValue
            });
    }
}
