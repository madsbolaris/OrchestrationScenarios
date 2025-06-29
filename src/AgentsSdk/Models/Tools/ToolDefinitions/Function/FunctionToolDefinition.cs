using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentsSdk.Conversion;
using AgentsSdk.Helpers;
using Microsoft.Extensions.AI;

namespace AgentsSdk.Models.Tools.ToolDefinitions.Function;

public class FunctionToolDefinition : ToolDefinition
{
    public override string Type => "function";

    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    public bool? Strict { get; set; }

    private Delegate? _method;
    private AIFunction? _cachedFunction;

    // Backing fields for schema behavior
    private JsonNode? _baseParameters;
    private JsonNode? _mergedParameters;

    // Cache-aware merged schema
    public virtual JsonNode? Parameters
    {
        get
        {
            _mergedParameters ??= SchemaMerger.Merge(_baseParameters, Overrides?.Parameters);

            return _mergedParameters;
        }
    }

    // Recalculate schema when overrides change
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

    public Delegate? Method
    {
        get => _method;
        set
        {
            _method = value;
            _cachedFunction = value is not null ? AIFunctionFactory.Create(value) : null;
        }
    }

    public static FunctionToolDefinition CreateToolDefinitionFromObjectMethod<T>(T instance, string methodName)
    {
        var methodInfo = typeof(T).GetMethod(methodName)!;

        // Get [KernelFunction("custom_name")] or fallback to actual method name
        var kernelAttr = methodInfo.GetCustomAttributes(typeof(Microsoft.SemanticKernel.KernelFunctionAttribute), false)
            .OfType<Microsoft.SemanticKernel.KernelFunctionAttribute>()
            .FirstOrDefault();
        var name = kernelAttr?.Name ?? methodInfo.Name;

        // Get [Description("...")] or fallback
        var descAttr = methodInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .OfType<DescriptionAttribute>()
            .FirstOrDefault();
        var description = descAttr?.Description ?? "No description available.";

        // Create delegate from method
        var methodDelegate = Delegate.CreateDelegate(
            typeof(Func<,>).MakeGenericType(
                methodInfo.GetParameters().First().ParameterType,
                methodInfo.ReturnType),
            instance,
            methodInfo
        );

        // Convert to AIFunction
        var aiFunction = AIFunctionFactory.Create(methodDelegate, name, description);
        var baseParameters = JsonNode.Parse(aiFunction.JsonSchema.GetRawText());

        return new FunctionToolDefinition
        {
            Name = name,
            Description = description,
            Method = methodDelegate,
            _baseParameters = baseParameters
        };
    }

    internal virtual ToolMetadata ToToolMetadata()
    {
        return new ToolMetadata
        {
            Name = Name,
            Type = Type,
            Description = Description,
            Parameters = Parameters,
            Executor = Method is not null
                ? async (input) =>
                {
                    var normalizedInput = ToolArgumentNormalizer.NormalizeArguments(Parameters, input);
                    return await _cachedFunction!.InvokeAsync(new(arguments: normalizedInput));
                }
                : null
        };
    }
}
