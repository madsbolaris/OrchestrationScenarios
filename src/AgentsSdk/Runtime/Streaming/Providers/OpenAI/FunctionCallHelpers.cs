namespace AgentsSdk.Runtime.Streaming.Providers.OpenAI;

using Microsoft.Extensions.AI;
using AgentsSdk.Models.Messages.Content;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentsSdk.Models.Tools.ToolDefinitions;

internal static class FunctionCallHelpers
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

    public static string ResolveName(string toolCallName, List<AgentToolDefinition>? tools)
    {
        if (tools is null || tools.Count == 0)
        {
            throw new InvalidOperationException("No tools provided to resolve function name.");
        }

        foreach (var tool in tools)
        {
            if (tool.Type.EndsWith(toolCallName))
            {
                return tool.Type;
            }
        }

        throw new InvalidOperationException($"Tool with name '{toolCallName}' not found in the provided tools.");
    }
}
