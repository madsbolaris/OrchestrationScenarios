namespace AgentsSdk.Conversion;

using AgentsSdk.Models.Tools;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using System.Text.Json;

internal static class ToolConversion
{
    public static AIFunction ToAIFunction(ToolMetadata tool)
    {
        if (tool.Executor is null)
        {
            throw new InvalidOperationException($"Tool '{tool.Name}' is not executable client side.");
        }

        return AIFunctionFactory.Create(tool.Executor, tool.Name, tool.Description ?? "");
    }

    public static ResponseTool ToResponseTool(ToolMetadata tool)
    {
        if (tool.Type == "Microsoft.BingGrounding")
        {
            return ResponseTool.CreateWebSearchTool();
        }

        var aiFunction = ToAIFunction(tool);
        var json = tool.Parameters?.ToJsonString() ?? "{}";
        return ResponseTool.CreateFunctionTool(
            aiFunction.Name,
            aiFunction.Description,
            BinaryData.FromString(json)
        );
    }

}
