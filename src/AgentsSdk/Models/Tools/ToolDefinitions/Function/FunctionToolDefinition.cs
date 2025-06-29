using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentsSdk.Conversion;
using Microsoft.Extensions.AI;

namespace AgentsSdk.Models.Tools.ToolDefinitions.Function;

public class FunctionToolDefinition : ToolDefinition
{
    public override string Type => "function";

    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    /// <summary>
    /// JSON Schema describing the parameters. Stored as raw JSON.
    /// </summary>
    public JsonNode? Parameters { get; set; }

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
        var parameters = JsonNode.Parse(aiFunction.JsonSchema.GetRawText());

        return new FunctionToolDefinition
        {
            Name = name,
            Description = description,
            Method = methodDelegate,
            Parameters = parameters,
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
                    return await _cachedFunction!.InvokeAsync(new(arguments: input));
                }
                : null
        };
    }
}
