// ClientSideToolDefinition.cs
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentsSdk.Helpers;

namespace AgentsSdk.Models.Tools.ToolDefinitions;

public abstract class ClientSideToolDefinition : ToolDefinition
{
    private readonly string _type;
    public override string Type => _type;

    protected ClientSideToolDefinition(string type)
    {
        _type = type;
    }

    protected JsonNode? _baseParameters;
    private JsonNode? _mergedParameters;

    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }

    public JsonNode? Parameters
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

    protected Func<Dictionary<string, object?>, Task<object?>>? _executor;

    public void SetExecutor(Func<Dictionary<string, object?>, Task<object?>> executor)
    {
        _executor = executor;
    }

    protected virtual JsonNode? FilteredParameters =>
        Parameters is JsonObject obj
            ? FilterObject(obj)
            : Parameters;

    private static JsonObject FilterObject(JsonObject original)
    {
        var filtered = new JsonObject();
        if (original.TryGetPropertyValue("properties", out var propsNode) &&
            propsNode is JsonObject props)
        {
            var filteredProps = new JsonObject();
            foreach (var (key, value) in props)
            {
                if (value is JsonObject propObj)
                {
                    filteredProps[key] = InputParameterFilter.Filter(propObj);
                }
            }
            filtered["type"] = "object";
            filtered["properties"] = filteredProps;
            return filtered;
        }

        return original;
    }

    protected static async Task<object?> ConvertAsync(Task task)
    {
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }

    internal virtual ToolMetadata ToToolMetadata()
    {
        return new ToolMetadata
        {
            Name = Name,
            Type = Type,
            Description = Description,
            Parameters = FilteredParameters,
            Executor = _executor is not null
                ? async (input) =>
                {
                    var effectiveSchema = Overrides?.Parameters ?? Parameters;
                    var normalized = ToolArgumentNormalizer.NormalizeArguments(effectiveSchema, input);
                    var results = await _executor(normalized);
                    return results;
                }
                : null
        };
    }
} 
