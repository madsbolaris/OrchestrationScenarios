// File: Conversion/ToResponseConverter.cs
// Namespace: OrchestrationScenarios.Conversion

using OpenAI.Responses;
using OrchestrationScenarios.Models.Tools.ToolDefinitions.Function;
using OrchestrationScenarios.Models.Tools.ToolDefinitions.BingGrounding;
using OrchestrationScenarios.Models.Tools.ToolDefinitions;
using OrchestrationScenarios.Models.Messages;

namespace OrchestrationScenarios.Conversion;

public static class ToResponseConverter
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
        return MicrosoftExtensionsAIToResponseConverter.Convert(aiFunction);
    }

    private static ResponseTool CreateBingTool()
    {
        return ResponseTool.CreateWebSearchTool();
    }
}
