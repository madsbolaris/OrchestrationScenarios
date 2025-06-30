using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentsSdk.Conversion;
using Microsoft.Extensions.AI;

namespace AgentsSdk.Models.Tools.ToolDefinitions.Function;

public class FunctionToolDefinition : ClientSideToolDefinition
{
    public bool? Strict { get; set; }

    private Delegate? _method;
    private AIFunction? _cachedFunction;

    public Delegate? Method
    {
        get => _method;
        set
        {
            _method = value;
            _cachedFunction = value is not null ? AIFunctionFactory.Create(value) : null;
        }
    }

    public FunctionToolDefinition(string name, string? description = null)
        : base("function")
    {
        Name = name;
        Description = description;
    }

    public static FunctionToolDefinition CreateToolDefinitionFromObjectMethod<T>(T instance, string methodName)
    {
        var methodInfo = typeof(T).GetMethod(methodName)!;

        var kernelAttr = methodInfo.GetCustomAttributes(typeof(Microsoft.SemanticKernel.KernelFunctionAttribute), false)
            .OfType<Microsoft.SemanticKernel.KernelFunctionAttribute>()
            .FirstOrDefault();
        var name = kernelAttr?.Name ?? methodInfo.Name;

        var descAttr = methodInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .OfType<DescriptionAttribute>()
            .FirstOrDefault();
        var description = descAttr?.Description ?? "No description available.";

        var methodDelegate = Delegate.CreateDelegate(
            typeof(Func<,>).MakeGenericType(
                methodInfo.GetParameters().First().ParameterType,
                methodInfo.ReturnType),
            instance,
            methodInfo
        );

        var aiFunction = AIFunctionFactory.Create(methodDelegate, name, description);
        var baseParameters = JsonNode.Parse(aiFunction.JsonSchema.GetRawText());

        var tool = new FunctionToolDefinition(name, description)
        {
            Method = methodDelegate,
            _baseParameters = baseParameters,
            _cachedFunction = aiFunction
        };

        return tool;
    }

    internal override ToolMetadata ToToolMetadata()
    {
        return new ToolMetadata
        {
            Name = Name,
            Type = Type,
            Description = Description,
            Parameters = Parameters,
            Executor = _cachedFunction is not null
                ? async (input) =>
                {
                    var normalized = ToolArgumentNormalizer.NormalizeArguments(Parameters, input);
                    return await _cachedFunction.InvokeAsync(new(arguments: normalized));
                }
                : null
        };
    }
}
