namespace OrchestrationScenarios.Helpers;

using Microsoft.Extensions.AI;
using OrchestrationScenarios.Models.Messages.Content;

public static class FunctionCallExecutor
{
    public static async Task<object?> ExecuteAsync(ToolCallContent toolCall, Dictionary<string, AIFunction> aiFunctions)
    {
        if (toolCall.Name is null)
            throw new ArgumentException("FunctionCallContent.Name is null");

        var function = aiFunctions.GetValueOrDefault(toolCall.Name) ?? throw new InvalidOperationException($"Function '{toolCall.Name}' not found");
        var args = toolCall.Arguments;

        var result = await function.InvokeAsync(new (args));
        return result;
    }
}
