namespace AgentsSdk.Runtime;

using Microsoft.Extensions.AI;
using AgentsSdk.Models.Messages.Content;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class FunctionCallExecutor
{
    public static async Task<object?> ExecuteAsync(ToolCallContent toolCall, Dictionary<string, AIFunction> aiFunctions)
    {
        if (toolCall.Name is null)
            throw new ArgumentException("FunctionCallContent.Name is null");

        var function = aiFunctions.GetValueOrDefault(toolCall.Name) 
            ?? throw new InvalidOperationException($"Function '{toolCall.Name}' not found");

        Dictionary<string, object?> args;

        if (function.UnderlyingMethod!.GetParameters().Length == 1 &&
            function.UnderlyingMethod.GetParameters()[0].ParameterType == typeof(JsonNode) &&
            toolCall.Arguments is not null)
        {
            var jsonObject = new JsonObject();
            foreach (var kvp in toolCall.Arguments)
            {
                jsonObject[kvp.Key] = JsonSerializer.SerializeToNode(kvp.Value);
            }

            args = new Dictionary<string, object?> { { "input", jsonObject } };
        }
        else
        {
            args = toolCall.Arguments!;
        }

        var result = await function.InvokeAsync(new(args));
        return result;
    }
}
