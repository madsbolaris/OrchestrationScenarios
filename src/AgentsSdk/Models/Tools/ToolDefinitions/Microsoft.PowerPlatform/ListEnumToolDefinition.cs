using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Identity;

namespace AgentsSdk.Models.Tools.ToolDefinitions.PowerPlatform;

public class ListEnumToolDefinition : ClientSideToolDefinition
{
    private readonly string _type;
    public override string Type => _type;

    private readonly Dictionary<string, ListEnumParameterReference> _invocationParameters;
    private readonly ListEnumSettings _settings;

    private static readonly string RequestUrl =
        "https://5a7d35b729b3e0eca2898fd0ce3aab.0a.environment.api.powerplatform.com/powerautomate/apis/shared_excelonlinebusiness/connections/845f7ef2b3e14296a6e0bc77a564a9b2/listEnum?api-version=1";

    public ListEnumToolDefinition(
        string type,
        string name,
        JsonObject inputSchema,
        Dictionary<string, ListEnumParameterReference> invocationParameters,
        ListEnumSettings settings)
        : base(type)
    {
        _type = type;
        Name = name;
        Description = $"Enumerates {settings.ValuePath} and {settings.ValueTitle} for {settings.OperationId}";
        _invocationParameters = invocationParameters;
        _settings = settings;

        if (inputSchema is { Count: > 0 })
        {
            _baseParameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = inputSchema
            };
        }

        _executor = ExecuteAsync;
    }

    protected override JsonNode? FilteredParameters
    {
        get
        {
            if (Parameters is not JsonObject obj)
                return Parameters;

            if (!obj.TryGetPropertyValue("properties", out var propsNode) || propsNode is not JsonObject props)
                return obj;

            var filteredProps = new JsonObject();
            foreach (var (key, value) in props)
            {
                if (value is JsonObject propObj &&
                    (!propObj.TryGetPropertyValue("readonly", out var readonlyNode) ||
                    !string.Equals(readonlyNode?.ToString(), "true", StringComparison.OrdinalIgnoreCase)))
                {
                    filteredProps[key] = JsonNode.Parse(propObj.ToJsonString())!;
                }
            }

            return new JsonObject
            {
                ["type"] = "object",
                ["properties"] = filteredProps
            };
        }
    }


    private async Task<object?> ExecuteAsync(Dictionary<string, object?> inputDict)
    {
        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(
            new Azure.Core.TokenRequestContext(["https://service.flow.microsoft.com/.default"]));

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var wrappedParams = new JsonObject();
        foreach (var (key, reference) in _invocationParameters)
        {
            wrappedParams[key] = reference.ParameterName != null
                ? new JsonObject { ["parameterReference"] = reference.ParameterName }
                : new JsonObject { ["value"] = JsonSerializer.SerializeToNode(reference.StaticValue) };
        }

        var runtimeInputs = new JsonObject();
        foreach (var (key, value) in inputDict)
        {
            runtimeInputs[key] = JsonSerializer.SerializeToNode(value);
        }

        var body = new JsonObject
        {
            ["parameters"] = runtimeInputs,
            ["dynamicInvocationDefinition"] = new JsonObject
            {
                ["operationId"] = _settings.OperationId,
                ["parameters"] = wrappedParams,
                ["itemsPath"] = _settings.ValueCollection,
                ["itemValuePath"] = _settings.ValuePath,
                ["itemTitlePath"] = _settings.ValueTitle
            }
        };

        var response = await httpClient.PostAsync(
            RequestUrl,
            new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return json;

        var root = JsonNode.Parse(json);
        if (root is not JsonObject rootObj)
            return json;

        if (_settings.ReadOnlyExpectedValue is not null &&
            rootObj.TryGetPropertyValue("value", out var valueArrayNode) &&
            valueArrayNode is JsonArray valueArray)
        {
            var filteredArray = new JsonArray();
            foreach (var item in valueArray)
            {
                if (item is JsonObject obj &&
                    obj.TryGetPropertyValue("value", out var valNode) &&
                    valNode?.ToJsonString() == JsonSerializer.Serialize(_settings.ReadOnlyExpectedValue))
                {
                    filteredArray.Add(JsonNode.Parse(obj.ToJsonString())!.AsObject());
                }
            }

            rootObj["value"] = filteredArray;
        }

        return rootObj;
    }
}
