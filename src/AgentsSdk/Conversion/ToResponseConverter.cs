using OpenAI.Responses;
using AgentsSdk.Models.Tools.ToolDefinitions.Function;
using AgentsSdk.Models.Tools.ToolDefinitions.BingGrounding;
using AgentsSdk.Models.Tools.ToolDefinitions;
using AgentsSdk.Models.Messages;
using System.Text.Json.Nodes;

namespace AgentsSdk.Conversion;

internal static class ToResponseConverter
{
    public static ResponseTool Convert(AgentToolDefinition tool)
    {
        return tool switch
        {
            FunctionToolDefinition func => ConvertFunctionTool(func),
            BingGroundingToolDefinition => CreateBingTool(),
            _ => throw new NotSupportedException($"Unknown tool type: {tool.GetType().Name}")
        };
    }

    public static IEnumerable<ResponseItem> Convert(ChatMessage message)
    {
        var converted = ToMicrosoftExtensionsAIContentConverter.Convert(message);
        return MicrosoftExtensionsAIToResponseConverter.Convert(converted);
    }

    private static ResponseTool ConvertFunctionTool(FunctionToolDefinition tool)
    {
        var aiFunction = ToMicrosoftExtensionsAIContentConverter.ToAIFunction(tool);
        return MicrosoftExtensionsAIToResponseConverter.Convert(aiFunction, tool.Parameters ?? new JsonObject());
    }

    private static ResponseTool CreateBingTool()
    {
        return ResponseTool.CreateWebSearchTool();
    }
}
