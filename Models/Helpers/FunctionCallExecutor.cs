namespace OrchestrationScenarios.Models.Helpers;

using System.Text.Json;
using Microsoft.SemanticKernel;
using FunctionCallContent = ContentParts.ToolCallContent;

public static class FunctionCallExecutor
{
    public static async Task<object?> ExecuteAsync(FunctionCallContent functionCall, Kernel kernel)
    {
        if (functionCall.Name is null)
            throw new ArgumentException("FunctionCallContent.Name is null");

        var parts = functionCall.Name.Split('-');
        if (parts.Length != 2)
            throw new InvalidOperationException($"Expected function name in format 'plugin-function', got '{functionCall.Name}'");

        string pluginName = parts[0];
        string functionName = parts[1];

        var function = kernel.Plugins.GetFunction(pluginName, functionName) ?? throw new InvalidOperationException($"Function '{functionName}' not found in plugin '{pluginName}'");
        var args = JsonSerializer.Deserialize<Dictionary<string, object>>(functionCall.Arguments)
                   ?? throw new InvalidOperationException("Failed to deserialize arguments");

        var result = await function.InvokeAsync(kernel, new KernelArguments(args!));
        return result?.GetValue<object>();
    }
}
