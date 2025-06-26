using System.ComponentModel;
using System.Text.Json.Nodes;

namespace AgentsSdk.Models.Tools.ToolDefinitions.Function;

public class FunctionToolDefinition : AgentToolDefinition
{
    public override string Type => "function";

    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    /// <summary>
    /// JSON Schema describing the parameters. Stored as raw JSON.
    /// </summary>
    public JsonNode? Parameters { get; set; }

    public bool? Strict { get; set; }

    public Delegate? Method { get; set; }

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

        return new FunctionToolDefinition
        {
            Name = name,
            Description = description,
            Method = methodDelegate
        };
    }
}
