using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Identity;

namespace AgentsSdk.Models.Tools.ToolDefinitions.PowerPlatform;

public class ListEnumToolDefinition : ClientSideToolDefinition
{
    private readonly string _operationId;
    private readonly JsonObject _rawInvocationParameters;
    private readonly string _valueCollection;
    private readonly string _valuePath;
    private readonly string _valueTitle;
    public override string Type => _type;

    private readonly object? _readOnlyExpectedValue;

    private static readonly string RequestUrl =
        "https://5a7d35b729b3e0eca2898fd0ce3aab.0a.environment.api.powerplatform.com/powerautomate/apis/shared_excelonlinebusiness/connections/845f7ef2b3e14296a6e0bc77a564a9b2/listEnum?api-version=1";

    public ListEnumToolDefinition(
        string type,
        string name,
        string operationId,
        JsonObject inputSchema,
        JsonObject rawInvocationParameters,
        string valueCollection,
        string valuePath,
        string valueTitle,
        object? readOnlyExpectedValue = null)
        : base(type)
    {
        _type = type;
        Name = name;
        Description = $"Enumerates values for {operationId}";

        _operationId = operationId;
        _rawInvocationParameters = rawInvocationParameters;
        _valueCollection = valueCollection;
        _valuePath = valuePath;
        _valueTitle = valueTitle;
        _readOnlyExpectedValue = readOnlyExpectedValue;

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

    private readonly string _type;

    private async Task<object?> ExecuteAsync(Dictionary<string, object?> inputDict)
    {
        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(
            new Azure.Core.TokenRequestContext(["https://service.flow.microsoft.com/.default"]));

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var wrappedParams = new JsonObject();
        foreach (var (key, val) in _rawInvocationParameters)
        {
            if (val is JsonObject obj && obj.TryGetPropertyValue("parameter", out var refNode))
            {
                wrappedParams[key] = new JsonObject
                {
                    ["parameterReference"] = refNode?.GetValue<string>()
                };
            }
            else
            {
                wrappedParams[key] = new JsonObject
                {
                    ["value"] = JsonSerializer.SerializeToNode(val)
                };
            }
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
                ["operationId"] = _operationId,
                ["parameters"] = wrappedParams,
                ["itemsPath"] = _valueCollection,
                ["itemValuePath"] = _valuePath,
                ["itemTitlePath"] = _valueTitle
            }
        };

        var response = await httpClient.PostAsync(
            RequestUrl,
            new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"ListEnum call failed: {response.StatusCode}\n{json}");

        var root = JsonNode.Parse(json);
        if (root is not JsonObject rootObj)
            return json;

        // Apply filtering if readonly + default were provided
        if (_readOnlyExpectedValue is not null &&
            rootObj.TryGetPropertyValue("value", out var valueArrayNode) &&
            valueArrayNode is JsonArray valueArray)
        {
            var filteredArray = new JsonArray();
            foreach (var item in valueArray)
            {
                if (item is not JsonObject obj)
                    continue;

                if (obj.TryGetPropertyValue("value", out var valNode) &&
                    valNode?.ToJsonString() == JsonSerializer.Serialize(_readOnlyExpectedValue))
                {
                    var clone = JsonNode.Parse(obj.ToJsonString())!.AsObject();
                    filteredArray.Add(clone);
                }

            }

            rootObj["value"] = filteredArray;
        }

        return rootObj;
    }
}
