namespace AgentsSdk.Runtime.Streaming.Providers.OpenAI;

using AgentsSdk.Models.Messages.Content;
using AgentsSdk.Models.Tools;

internal static class FunctionCallHelpers
{
    public static async Task<object?> ExecuteAsync(ToolCallContent toolCall, Dictionary<string, ToolMetadata> tools)
    {
        if (toolCall.Name is null)
            throw new ArgumentException("FunctionCallContent.Name is null");

        if (!tools.TryGetValue(toolCall.Name, out var metadata))
            throw new InvalidOperationException($"Function '{toolCall.Name}' not found");

        if (metadata.Executor is null)
            throw new InvalidOperationException($"Tool '{toolCall.Name}' is not executable");

        return await metadata.Executor(toolCall.Arguments!);
    }

    public static string ResolveName(string toolCallName, List<ToolMetadata> tools)
    {
        foreach (var tool in tools)
        {
            if (tool.Type.EndsWith(toolCallName))
                return tool.Type;

            if (tool.Name == toolCallName)
                return tool.Name;
        }

        throw new InvalidOperationException($"Tool with name '{toolCallName}' not found in the provided tools.");

    }
}
