using OpenAI.Responses;
using AgentsSdk.Models.Tools.ToolDefinitions.Function;
using AgentsSdk.Models.Tools.ToolDefinitions.BingGrounding;
using AgentsSdk.Models.Tools.ToolDefinitions;
using AgentsSdk.Models.Messages;
using AgentsSdk.Models.Tools.ToolDefinitions.PowerPlatform;

namespace AgentsSdk.Conversion;

internal static class ToResponseConverter
{
    public static ResponseTool Convert(ToolDefinition tool)
    {
        return tool switch
        {
            PowerPlatformToolDefinition power => ToolConversion.ToResponseTool(power.ToToolMetadata()),
            FunctionToolDefinition func => ToolConversion.ToResponseTool(func.ToToolMetadata()),
            BingGroundingToolDefinition bing => ToolConversion.ToResponseTool(bing.ToToolMetadata()),
            _ => throw new NotSupportedException($"Unknown tool type: {tool.GetType().Name}")
        };
    }

    public static IEnumerable<ResponseItem> Convert(ChatMessage message)
    {
        var converted = ToMicrosoftExtensionsAIContentConverter.Convert(message);
        return MicrosoftExtensionsAIToResponseConverter.Convert(converted);
    }
}
