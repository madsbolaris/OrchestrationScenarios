using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Identity;
using AgentsSdk.Models.Tools.ToolDefinitions.Function;
using AgentsSdk.Helpers;

namespace AgentsSdk.Models.Tools.ToolDefinitions.PowerPlatform;

public class ListEnumToolDefinition : FunctionToolDefinition
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
    
    public ListEnumToolDefinition(
        string type,
        string name,
        string operationId,
        JsonObject inputSchema,              // Merged properties -> used in _baseParameters
        JsonObject rawInvocationParameters,  // Original dynamicValues["parameters"]
        string valueCollection,
        string valuePath,
        string valueTitle)
    {
        _type = type;
        Name = name;
        Description = $"Enumerates values for {operationId}";

        if (inputSchema is { Count: > 0 })
        {
            _baseParameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = inputSchema
            };
        }

        Method = (Func<Dictionary<string, object?>, Task<object?>>)(async (inputDict) =>
        {
            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(
                new Azure.Core.TokenRequestContext(["https://service.flow.microsoft.com/.default"]));

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            // Reconstruct wrappedParams from rawInvocationParameters
            var wrappedParams = new JsonObject();
            foreach (var (key, val) in rawInvocationParameters)
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
                    ["operationId"] = operationId,
                    ["parameters"] = wrappedParams,
                    ["itemsPath"] = valueCollection,
                    ["itemValuePath"] = valuePath,
                    ["itemTitlePath"] = valueTitle
                }
            };

            var response = await httpClient.PostAsync(
                RequestUrl,
                new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"ListEnum call failed: {response.StatusCode}\n{json}");

            return json;
        });
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

                    var result = Method.DynamicInvoke(normalized);
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

    private static readonly string RequestUrl =
        "https://5a7d35b729b3e0eca2898fd0ce3aab.0a.environment.api.powerplatform.com/powerautomate/apis/shared_excelonlinebusiness/connections/845f7ef2b3e14296a6e0bc77a564a9b2/listEnum?api-version=1";
}
